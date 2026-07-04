using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WarehouseManagementSystem.API.Integration.Configuration;
using WarehouseManagementSystem.API.Integration.Contracts;
using WarehouseManagementSystem.Infrastructure.Integration;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Integration;

/// <summary>
/// Background service that consumes "DocumentConfirmed" messages from RabbitMQ and creates shipment requests in the database.
/// </summary>
public class ShippingDocumentConfirmedConsumer : BackgroundService
{
    public const string ConsumerName = "ShippingDocumentConfirmedConsumer";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly IRabbitMqTopologyConfigurator _topologyConfigurator;
    private readonly MessagingOptions _options;
    private readonly ILogger<ShippingDocumentConfirmedConsumer> _logger;

    public ShippingDocumentConfirmedConsumer(
        IServiceScopeFactory scopeFactory,
        IRabbitMqConnectionFactory connectionFactory,
        IRabbitMqTopologyConfigurator topologyConfigurator,
        IOptions<MessagingOptions> options,
        ILogger<ShippingDocumentConfirmedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _connectionFactory = connectionFactory;
        _topologyConfigurator = topologyConfigurator;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Executes the background service to consume messages from RabbitMQ.
    /// </summary>
    /// <param name="stoppingToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the background service execution.</returns>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Messaging is disabled. Shipping consumer will not start.");
            return Task.CompletedTask;
        }

        return Task.Run(() => Consume(stoppingToken), stoppingToken);
    }

    /// <summary>
    /// Consumes messages from the RabbitMQ queue and processes them.
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    private void Consume(CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();
        _topologyConfigurator.EnsureTopology(channel);
        channel.BasicQos(0, _options.Shipping.PrefetchCount, false);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (_, args) =>
        {
            var handled = HandleMessageAsync(args, ct).GetAwaiter().GetResult();
            if (handled)
            {
                channel.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
        };

        channel.BasicConsume(
            queue: _options.Shipping.DocumentConfirmedQueue,
            autoAck: false,
            consumer: consumer);

        while (!ct.IsCancellationRequested)
        {
            Thread.Sleep(500);
        }
    }
    /// <summary>
    /// Handles a received message by deserializing it, checking for duplicates, and creating a shipment request in the database.
    /// </summary>
    /// <param name="args">The arguments containing the message data.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the message was handled successfully.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<bool> HandleMessageAsync(BasicDeliverEventArgs args, CancellationToken ct)
    {
        try
        {
            var payload = Encoding.UTF8.GetString(args.Body.ToArray());
            var message = JsonSerializer.Deserialize<DocumentConfirmedIntegrationEvent>(payload, SerializerOptions)
                          ?? throw new InvalidOperationException("Cannot deserialize DocumentConfirmedIntegrationEvent.");

            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();

            var alreadyProcessed = await dbContext.ProcessedMessages
                .AnyAsync(x => x.Consumer == ConsumerName && x.MessageId == message.MessageId, ct);

            if (alreadyProcessed)
            {
                _logger.LogInformation("Shipping consumer skipped duplicate message {MessageId}.", message.MessageId);
                return true;
            }

            dbContext.ShippingShipments.Add(new ShippingShipment
            {
                Id = Guid.NewGuid(),
                DocumentId = message.DocumentId,
                DocumentNumber = message.DocumentNumber,
                DocumentType = message.DocumentType,
                SourceWarehouseId = message.SourceWarehouseId,
                TargetWarehouseId = message.TargetWarehouseId,
                MessageId = message.MessageId,
                CorrelationId = message.CorrelationId,
                RequestedAt = message.OccurredAt,
                CreatedAt = DateTimeOffset.UtcNow,
                Status = "Requested"
            });

            dbContext.ProcessedMessages.Add(new ProcessedMessage
            {
                Id = Guid.NewGuid(),
                Consumer = ConsumerName,
                MessageId = message.MessageId,
                MessageType = nameof(DocumentConfirmedIntegrationEvent),
                CorrelationId = message.CorrelationId,
                ProcessedAt = DateTimeOffset.UtcNow
            });

            await dbContext.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Shipping consumer created shipment request for document {DocumentId} ({DocumentNumber}).",
                message.DocumentId,
                message.DocumentNumber);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shipping consumer failed to handle a DocumentConfirmed message.");
            return false;
        }
    }
}
