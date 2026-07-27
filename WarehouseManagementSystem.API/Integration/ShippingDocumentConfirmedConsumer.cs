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
        // BROKER CONNECTION: tutaj consumer łączy się z brokerem RabbitMQ.
        using var connection = _connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();
        _topologyConfigurator.EnsureTopology(channel);
        channel.BasicQos(0, _options.Shipping.PrefetchCount, false);
        // Prefetch ogranicza liczbę wiadomości "w locie" na konsumenta. To pomaga odróżnić normalny backlog
        // od sytuacji, w której consumer się dławi i przestaje nadążać z przetwarzaniem.

        // CONSUMER OBJECT: EventingBasicConsumer reprezentuje odbiorcę wiadomości z queue.
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (_, args) =>
        {
            // CONSUME: tu zaczyna się odczyt i obsługa wiadomości pobranej z queue.
            // TODO(RECRUITMENT): Zastap blokowanie przez AsyncEventingBasicConsumer. GetResult blokuje
            // watek dispatchera; przy wolnej bazie ogranicza throughput i utrudnia graceful shutdown.
            var handled = HandleMessageAsync(args, ct).GetAwaiter().GetResult();
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
            // TODO(RECRUITMENT): Rozroznij bledy transient (timeout bazy/sieci -> retry z opoznieniem)
            // od permanentnych (zly JSON/kontrakt -> DLQ). Teraz kazdy wyjatek od razu trafia do DLQ.
            var retries = ConsumerRetryPolicy.GetRetryCount(args.BasicProperties.Headers);
            var retryPolicy = new ConsumerRetryPolicy(_options.Shipping.MaxRetryAttempts);
            if (retryPolicy.ShouldRetry(retries))
            {
                var properties = CopyProperties(channel, args.BasicProperties);
                properties.Headers[ConsumerRetryPolicy.RetryCountHeader] = retryPolicy.NextRetryCount(retries);
                properties.Headers[ConsumerRetryPolicy.LastAttemptAtHeader] = DateTimeOffset.UtcNow.ToString("O");
                channel.BasicPublish(_options.Shipping.DocumentConfirmedRetryExchange, _options.Shipping.DocumentConfirmedRoutingKey, properties, args.Body);
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
            // MESSAGE: args.Body to konkretna wiadomość odebrana z queue przez consumera.
            var payload = Encoding.UTF8.GetString(args.Body.ToArray());
            var message = JsonSerializer.Deserialize<DocumentConfirmedIntegrationEvent>(payload, SerializerOptions)
                          ?? throw new InvalidOperationException("Cannot deserialize DocumentConfirmedIntegrationEvent.");

            // TODO(RECRUITMENT): Waliduj semantyke kontraktu (puste Guidy, ilosci <= 0, wymagane pola)
            // oraz wersje schematu. Poprawny JSON nie musi byc poprawnym zdarzeniem biznesowym.

            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();

            var alreadyProcessed = await dbContext.ProcessedMessages
                .AnyAsync(x => x.Consumer == ConsumerName && x.MessageId == message.MessageId, ct);

            if (alreadyProcessed)
            {
                // To jest klasyczny idempotent consumer pattern. Zakładamy, że broker może dostarczyć
                // tę samą wiadomość wiele razy, więc sprawdzamy MessageId zamiast ufać exactly-once.
                _logger.LogInformation("Shipping consumer skipped duplicate message {MessageId}.", message.MessageId);
                return true;
            }

            //  TODO(RECRUITMENT): Usun race condition check-then-insert. Dwa rownolegle delivery moga oba
            // przejsc AnyAsync; unikalny indeks ochroni baze, ale drugi SaveChanges rzuci wyjatek i poprawny
            // duplikat trafi do DLQ. Konflikt unique potraktuj jako ACK albo uzyj atomowego upsertu.

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
            catch (DbUpdateException ex) when (IsProcessedMessageDuplicate(ex))
            {
                _logger.LogInformation("Shipping consumer skipped concurrently processed duplicate message {MessageId}.", message.MessageId);
                return true;
            }
            // TODO(RECRUITMENT): Dodaj AggregateVersion/Sequence do eventu i zapamietuj ostatnia wersje
            // per DocumentId. Zdefiniuj polityke dla luk: buforowanie, retry albo reconciliation.
            // TODO(RECRUITMENT): Dodaj okresowy reconciliation job porownujacy potwierdzone dokumenty WMS
            // z ShippingShipments oraz kontrolowany rebuild projekcji. To wykrywa missing events, ktorych
            // retry i DLQ nie zobacza, np. przez blad danych albo utracony binding.
            // TODO(RECRUITMENT): Przetestuj crash po zapisie przed ACK oraz dwa rownolegle delivery tego
            // samego MessageId. Oczekiwany wynik to jeden efekt biznesowy i poprawny ACK duplikatu.
            // Tego flow nie chroni jeszcze jawna walidacja kolejności zdarzeń typu "Cancelled przyszło przed Confirmed".
            // Jeśli dojdziesz do wielu eventów dla tego samego dokumentu, to właśnie tutaj pojawia się problem
            // out-of-order delivery i potrzeba wersjonowania / sekwencji biznesowej.

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

    private static bool IsProcessedMessageDuplicate(DbUpdateException exception)
    {
        return exception.InnerException?.Message.Contains("IX_ProcessedMessages_Consumer_MessageId", StringComparison.OrdinalIgnoreCase) == true ||
        exception.InnerException?.Message.Contains("ProcessedMessages.Consumer, ProcessedMessages.MessageId", StringComparison.OrdinalIgnoreCase) == true;
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
