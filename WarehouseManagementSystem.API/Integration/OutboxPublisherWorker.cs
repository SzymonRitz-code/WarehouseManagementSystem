using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using WarehouseManagementSystem.API.Integration.Configuration;
using WarehouseManagementSystem.Infrastructure.Integration;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Integration;

/// <summary>
/// Background service that publishes pending outbox messages to RabbitMQ.
/// </summary>
public class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly IRabbitMqTopologyConfigurator _topologyConfigurator;
    private readonly MessagingOptions _options;
    private readonly ILogger<OutboxPublisherWorker> _logger;

    public OutboxPublisherWorker(
        IServiceScopeFactory scopeFactory,
        IRabbitMqConnectionFactory connectionFactory,
        IRabbitMqTopologyConfigurator topologyConfigurator,
        IOptions<MessagingOptions> options,
        ILogger<OutboxPublisherWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _connectionFactory = connectionFactory;
        _topologyConfigurator = topologyConfigurator;
        _options = options.Value;
        _logger = logger;
    }
    
    /// <summary>
    /// Executes the background service.
    /// </summary>
    /// <param name="stoppingToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the background service execution.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Messaging is disabled. Outbox publisher will not start.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox publisher iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PublishPollIntervalSeconds), stoppingToken);
        }
    }
    /// <summary>
    /// Publishes pending outbox messages to RabbitMQ.
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task PublishPendingMessagesAsync(CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();
        _topologyConfigurator.EnsureTopology(channel);

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();

        var messages = await dbContext.OutboxMessages
            .Where(x => x.Status == OutboxMessageStatus.Pending || x.Status == OutboxMessageStatus.Failed)
            .OrderBy(x => x.OccurredAt)
            .Take(_options.PublishBatchSize)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.MessageId = message.MessageId.ToString();
                properties.Type = message.Type;
                properties.CorrelationId = message.CorrelationId?.ToString();
                properties.Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds());

                var body = System.Text.Encoding.UTF8.GetBytes(message.Payload);
                channel.BasicPublish(
                    exchange: _options.Exchanges.WmsEvents,
                    routingKey: message.RoutingKey,
                    basicProperties: properties,
                    body: body);

                message.Status = OutboxMessageStatus.Published;
                message.PublishedAt = DateTimeOffset.UtcNow;
                message.LastError = null;

                _logger.LogInformation("Published outbox message {MessageId} ({Type}).", message.MessageId, message.Type);
            }
            catch (Exception ex)
            {
                message.Status = OutboxMessageStatus.Failed;
                message.RetryCount++;
                message.LastError = ex.Message;

                _logger.LogWarning(ex, "Failed to publish outbox message {MessageId}.", message.MessageId);
            }
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
