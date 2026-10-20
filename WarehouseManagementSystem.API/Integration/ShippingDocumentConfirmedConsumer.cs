using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WarehouseManagementSystem.API.Integration.Configuration;
using WarehouseManagementSystem.Contracts;
using WarehouseManagementSystem.Infrastructure.Integration;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Integration;

/// <summary>
/// Background service that consumes "DocumentConfirmed" messages from RabbitMQ and creates shipment requests in the database.
/// </summary>
public class ShippingDocumentConfirmedConsumer : BackgroundService
{
    // CONSUMER: ten background service odbiera wiadomości z queue.
    public const string ConsumerName = "ShippingDocumentConfirmedConsumer";
    private const string LocalQueue = "wms.local.shipping.document-confirmed";
    private const string LocalRetryExchange = "wms.local.shipping.retry";

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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !_options.LocalShippingProjectionEnabled)
        {
            _logger.LogInformation("Messaging is disabled. Shipping consumer will not start.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await Consume(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { _logger.LogError(ex, "Shipping connection failed; retrying."); }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    /// <summary>
    /// Consumes messages from the RabbitMQ queue and processes them.
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    private async Task Consume(CancellationToken ct)
    {
        // BROKER CONNECTION: tutaj consumer łączy się z brokerem RabbitMQ.
        using var connection = _connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();
        _topologyConfigurator.EnsureTopology(channel);
        // This optional WMS projection owns a separate queue; it must never compete with FakeShipping.
        channel.ExchangeDeclare(LocalRetryExchange, ExchangeType.Direct, durable: true);
        channel.QueueDeclare(LocalQueue + ".dlq", true, false, false);
        channel.QueueBind(LocalQueue + ".dlq", _options.Exchanges.DeadLetter, LocalQueue);
        channel.QueueDeclare(LocalQueue, true, false, false, new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = _options.Exchanges.DeadLetter,
            ["x-dead-letter-routing-key"] = LocalQueue
        });
        channel.QueueBind(LocalQueue, _options.Exchanges.WmsEvents, _options.Shipping.DocumentConfirmedRoutingKey);
        channel.QueueDeclare(LocalQueue + ".retry", true, false, false, new Dictionary<string, object>
        {
            ["x-message-ttl"] = _options.Shipping.RetryDelaySeconds * 1000,
            ["x-dead-letter-exchange"] = "",
            ["x-dead-letter-routing-key"] = LocalQueue
        });
        channel.QueueBind(LocalQueue + ".retry", LocalRetryExchange, _options.Shipping.DocumentConfirmedRoutingKey);
        channel.BasicQos(0, _options.Shipping.PrefetchCount, false);
        // Prefetch ogranicza liczbę wiadomości "w locie" na konsumenta. To pomaga odróżnić normalny backlog
        // od sytuacji, w której consumer się dławi i przestaje nadążać z przetwarzaniem.

        // CONSUMER OBJECT: EventingBasicConsumer reprezentuje odbiorcę wiadomości z queue.
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, args) =>
        {
            // CONSUME: tu zaczyna się odczyt i obsługa wiadomości pobranej z queue.
            bool handled;
            try { handled = await HandleMessageAsync(args, ct); }
            catch (PermanentIntegrationException ex)
            {
                _logger.LogError(ex, "Invalid shipping contract; sending to DLQ.");
                channel.BasicNack(args.DeliveryTag, false, false);
                return;
            }
            if (handled)
            {
                // Ack wysyłamy dopiero po trwałym zapisie skutku biznesowego i wpisu do ProcessedMessages.
                // Gdyby proces padł wcześniej, broker dostarczy wiadomość ponownie - dokładnie dlatego
                // ten consumer jest napisany pod model at-least-once, a nie exactly-once.
                channel.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            // Requeue=false kieruje poison message do DLQ przez skonfigurowany dead-letter exchange.
            // To odpowiada na pytanie "co robisz z wiadomością, której nie da się poprawnie przetworzyć?".
            var retries = ConsumerRetryPolicy.GetRetryCount(args.BasicProperties.Headers);
            var retryPolicy = new ConsumerRetryPolicy(_options.Shipping.MaxRetryAttempts);
            if (retryPolicy.ShouldRetry(retries))
            {
                var properties = CopyProperties(channel, args.BasicProperties);
                properties.Headers[ConsumerRetryPolicy.RetryCountHeader] = retryPolicy.NextRetryCount(retries);
                properties.Headers[ConsumerRetryPolicy.LastAttemptAtHeader] = DateTimeOffset.UtcNow.ToString("O");
                try
                {
                    ConfirmedMessagePublisher.Publish(channel, LocalRetryExchange, _options.Shipping.DocumentConfirmedRoutingKey, properties, args.Body, TimeSpan.FromSeconds(_options.PublishConfirmTimeoutSeconds));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Retry publication failed; original delivery remains retryable.");
                    if (channel.IsOpen) channel.BasicNack(args.DeliveryTag, false, true);
                    return;
                }
                channel.BasicAck(args.DeliveryTag, multiple: false);
                _logger.LogWarning("Shipping consumer scheduled retry {RetryCount} for MessageId {MessageId}.", retryPolicy.NextRetryCount(retries), args.BasicProperties.MessageId);
                return;
            }

            _logger.LogError("Shipping consumer sent MessageId {MessageId} to DLQ after {RetryCount} retries.", args.BasicProperties.MessageId, retries);
            channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
        };

        // BASIC CONSUME:
        // QUEUE: _options.Shipping.DocumentConfirmedQueue
        // CONSUMER: consumer
        channel.BasicConsume(
            queue: LocalQueue,
            autoAck: false,
            consumer: consumer);

        await Task.Delay(Timeout.Infinite, ct);
    }
    /// <summary>
    /// Handles a received message by deserializing it, checking for duplicates, and creating a shipment request in the database.
    /// </summary>
    /// <param name="args">The arguments containing the message data.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the message was handled successfully.</returns>
    /// <exception cref="PermanentIntegrationException">The JSON or business contract is invalid or unsupported.</exception>
    public async Task<bool> HandleMessageAsync(BasicDeliverEventArgs args, CancellationToken ct)
    {
        try
        {
            // MESSAGE: args.Body to konkretna wiadomość odebrana z queue przez consumera.
            var payload = Encoding.UTF8.GetString(args.Body.ToArray());
            var message = JsonSerializer.Deserialize<DocumentConfirmedIntegrationEvent>(payload, SerializerOptions)
                          ?? throw new PermanentIntegrationException("Empty event.");
            DocumentConfirmedContract.Validate(message);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();

            // A confirmation is version 1 and immutable. The unique document key is its version checkpoint.
            if (await dbContext.ShippingShipments.AnyAsync(x => x.DocumentId == message.DocumentId, ct))
                return true;

            var alreadyProcessed = await dbContext.ProcessedMessages
                .AnyAsync(x => x.Consumer == ConsumerName && x.MessageId == message.MessageId, ct);

            if (alreadyProcessed)
            {
                // To jest klasyczny idempotent consumer pattern. Zakładamy, że broker może dostarczyć
                // tę samą wiadomość wiele razy, więc sprawdzamy MessageId zamiast ufać exactly-once.
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

            // Skutek biznesowy i znacznik przetworzenia zapisujemy razem. Jeśli consumer padnie przed
            // SaveChanges, nic się nie utrwali i wiadomość wróci. Jeśli padnie po SaveChanges, drugi przebieg
            // zobaczy ProcessedMessages i nie wykona efektu drugi raz.
            dbContext.ProcessedMessages.Add(new ProcessedMessage
            {
                Id = Guid.NewGuid(),
                Consumer = ConsumerName,
                MessageId = message.MessageId,
                MessageType = nameof(DocumentConfirmedIntegrationEvent),
                CorrelationId = message.CorrelationId,
                ProcessedAt = DateTimeOffset.UtcNow
            });

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                await using var verifyScope = _scopeFactory.CreateAsyncScope();
                var verify = verifyScope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();
                if (!await verify.ShippingShipments.AnyAsync(x => x.DocumentId == message.DocumentId, ct)) throw;
                _logger.LogInformation("Shipping consumer skipped concurrently processed document {DocumentId}.", message.DocumentId);
                return true;
            }

            _logger.LogInformation(
                "Shipping consumer created shipment request for document {DocumentId} ({DocumentNumber}).",
                message.DocumentId,
                message.DocumentNumber);

            return true;
        }
        catch (JsonException ex) { throw new PermanentIntegrationException(ex.Message); }
        catch (PermanentIntegrationException) { throw; }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shipping consumer failed to handle a DocumentConfirmed message.");
            return false;
        }
    }

    private static IBasicProperties CopyProperties(IModel channel, IBasicProperties source)
    {
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = source.MessageId;
        properties.Type = source.Type;
        properties.CorrelationId = source.CorrelationId;
        properties.Timestamp = source.Timestamp;
        properties.Headers = source.Headers is null ? new Dictionary<string, object>() : new Dictionary<string, object>(source.Headers);
        return properties;
    }
}
