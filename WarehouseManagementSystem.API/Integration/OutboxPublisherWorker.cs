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
    // PUBLISHER: ten worker pełni rolę publishera i wysyła wiadomości do brokera.
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
        // BROKER CONNECTION: tutaj publisher łączy się z brokerem RabbitMQ.
        // TODO(RECRUITMENT): Utrzymuj dlugowieczne polaczenie/kanal i dodaj reconnect z backoffem.
        // Tworzenie ich przy kazdym pollingu jest kosztowne, a chwilowa awaria brokera konczy cala iteracje.
        using var connection = _connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();
        _topologyConfigurator.EnsureTopology(channel);
        channel.ConfirmSelect();

        // TODO(RECRUITMENT): Wlacz publisher confirms i oznaczaj rekord jako Published dopiero po ACK brokera.
        // BasicPublish moze zakonczyc sie bez wyjatku, mimo ze broker nie utrwalil wiadomosci. Dodaj tez
        // mandatory=true + BasicReturn, bo brak pasujacego bindingu moze po cichu zgubic unroutable message.

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();

        var messages = await dbContext.OutboxMessages
            .Where(x => (x.Status == OutboxMessageStatus.Pending || x.Status == OutboxMessageStatus.Failed) &&
                        (x.NextAttemptAt == null || x.NextAttemptAt <= DateTimeOffset.UtcNow))
            // TODO(RECRUITMENT): Przy wielu instancjach atomowo claimuj rekordy (np. Processing + lease,
            // SELECT ... FOR UPDATE SKIP LOCKED albo optimistic concurrency). Sam SELECT pozwala dwom
            // publisherom pobrac i opublikowac ten sam batch.
            // Publikujemy w kolejności OccurredAt, żeby maksymalnie zachować porządek biznesowy
            // w obrębie jednego publishera. To nie daje globalnej gwarancji ordering między wszystkimi
            // konsumentami i partycjami - jeśli na rozmowie padnie temat strict ordering, to tu właśnie
            // zaczyna się ograniczenie obecnej implementacji.
            .OrderBy(x => x.OccurredAt)
            .Take(_options.PublishBatchSize)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                // Tworzy nowy obiekt BasicProperties, który pozwala ustawić różne właściwości wiadomości, takie jak:
                //  trwałość, identyfikator wiadomości, typ, identyfikator korelacji i znacznik czasu.
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.MessageId = message.MessageId.ToString();
                properties.Type = message.Type;
                properties.CorrelationId = message.CorrelationId?.ToString();
                properties.Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds());
                // MessageId i CorrelationId lecą w nagłówkach brokera, żeby downstream mógł:
                // 1) wykrywać duplikaty,
                // 2) wiązać logi i skutki biznesowe z jednym procesem end-to-end.

                var body = System.Text.Encoding.UTF8.GetBytes(message.Payload);
                // PUBLISH: to jest właściwy publish do exchange w brokerze.
                // EXCHANGE: _options.Exchanges.WmsEvents
                // ROUTING KEY: message.RoutingKey
                channel.BasicPublish(
                    exchange: _options.Exchanges.WmsEvents,
                    routingKey: message.RoutingKey,
                    basicProperties: properties,
                    body: body);

                // BasicPublish only writes to the client channel. A publisher confirm is the
                // broker acknowledgement that lets us safely move the outbox row forward.
                channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(_options.PublishConfirmTimeoutSeconds));

                message.Status = OutboxMessageStatus.Published;
                message.PublishedAt = DateTimeOffset.UtcNow;
                message.LastError = null;
                message.NextAttemptAt = null;

                _logger.LogInformation("Published outbox message {MessageId} ({Type}).", message.MessageId, message.Type);
            }
            catch (Exception ex)
            {
                OutboxMessageRetry.MarkFailed(message, ex, _options.MaxPublishAttempts, _options.PublishRetryDelaySeconds);
                // TODO(RECRUITMENT): Dodaj exponential backoff z jitterem, NextAttemptAt, MaxRetryCount
                // oraz stan Dead/Abandoned. Po przekroczeniu progu wyslij alert zamiast retry bez konca.
                // Prosty retry policy na poziomie outboxa: wiadomość zostaje w bazie jako Failed
                // i kolejna iteracja workera spróbuje opublikować ją ponownie. Nie ma tu jeszcze backoffu,
                // limitu prób ani eskalacji do alertu - to jest miejsce, gdzie naturalnie dochodzi się do
                // pytań o stuck messages i operacyjne monitorowanie.

                _logger.LogWarning(ex, "Failed to publish outbox message {MessageId}; attempt {RetryCount}, status {Status}.", message.MessageId, message.RetryCount, message.Status);
            }
        }

        if (messages.Count > 0)
        {
            // Uwaga rekrutacyjna: jeśli publish do RabbitMQ się uda, a proces padnie przed tym SaveChanges,
            // broker może dostarczyć tę samą wiadomość drugi raz po ponownym uruchomieniu workera.
            // To dlatego downstream musi zakładać at-least-once delivery i być idempotentny.
            await dbContext.SaveChangesAsync(ct);
        }

        // TODO(RECRUITMENT): Dodaj retencje/archiwizacje Published oraz metryki wieku najstarszej wiadomosci,
        // liczby Pending/Failed i czasu publikacji. To pozwala odroznic zwykly backlog od outage'u.
        // TODO(RECRUITMENT): Dodaj testy failure injection dla awarii przed publish, po publish przed
        // SaveChanges i po SaveChanges. W ten sposob udowodnisz at-least-once zamiast tylko je deklarowac.
    }
}
