using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using WarehouseManagementSystem.API.Integration.Configuration;
using WarehouseManagementSystem.Infrastructure.Integration;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Integration;

/// <summary>Publishes committed integration events with broker confirmation and exclusive batch ownership.</summary>
public sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopes, IRabbitMqConnectionFactory connections,
    IRabbitMqTopologyConfigurator topology, IOptions<MessagingOptions> options,
    ILogger<OutboxPublisherWorker> logger) : BackgroundService
{
    private readonly MessagingOptions _options = options.Value;
    private IConnection? _connection;
    private IModel? _channel;
    private static readonly Meter Meter = new("Wms.Outbox");
    private static readonly Histogram<double> PublishTime = Meter.CreateHistogram<double>("wms.outbox.publish.duration", "ms");
    private static readonly Histogram<double> OldestAge = Meter.CreateHistogram<double>("wms.outbox.oldest.age", "s");
    private static readonly Histogram<int> Backlog = Meter.CreateHistogram<int>("wms.outbox.backlog", "messages");
    private static readonly Counter<int> Failures = Meter.CreateCounter<int>("wms.outbox.failures");

    /// <summary>Reuses the broker session and reconnects with bounded exponential delay after an outage.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled) return;
        var failures = 0;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try { failures = await PublishPendingMessagesAsync(stoppingToken) ? 0 : failures + 1; }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    failures++;
                    CloseBroker();
                    logger.LogError(ex, "Outbox iteration failed; reconnect attempt {Attempt}.", failures);
                }
                await Task.Delay(TimeSpan.FromSeconds(failures == 0
                    ? Math.Max(1, _options.PublishPollIntervalSeconds)
                    : Math.Min(60, Math.Pow(2, Math.Min(failures, 6))) * (0.8 + Random.Shared.NextDouble() * 0.4)), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally { CloseBroker(); }
    }

    /// <summary>
    /// Locks a SQL Server batch until its result is committed, preventing concurrent publishers from
    /// selecting the same rows. A crash releases the transaction locks and leaves the rows retryable.
    /// Broker confirmation precedes the database commit, so consumers must tolerate redelivery.
    /// </summary>
    public async Task<bool> PublishPendingMessagesAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var messages = await db.OutboxMessages.FromSqlInterpolated($"""
            SELECT TOP ({Math.Max(1, _options.PublishBatchSize)}) * FROM OutboxMessages WITH (UPDLOCK, READPAST, READCOMMITTEDLOCK)
            WHERE Status IN ('Pending', 'Failed') AND (NextAttemptAt IS NULL OR NextAttemptAt <= {now})
            ORDER BY OccurredAt, Id
            """).ToListAsync(ct);

        var publicationFailed = false;
        foreach (var message in messages)
        {
            ct.ThrowIfCancellationRequested();
            var watch = Stopwatch.StartNew();
            try
            {
                var channel = GetChannel();
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.MessageId = message.MessageId.ToString();
                properties.Type = message.Type;
                properties.CorrelationId = message.CorrelationId?.ToString();
                properties.Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds());
                var returned = false;
                void OnReturn(object? sender, RabbitMQ.Client.Events.BasicReturnEventArgs args) => returned = true;
                channel.BasicReturn += OnReturn;
                try
                {
                    channel.BasicPublish(_options.Exchanges.WmsEvents, message.RoutingKey, true, properties,
                        System.Text.Encoding.UTF8.GetBytes(message.Payload));
                    channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(_options.PublishConfirmTimeoutSeconds));
                    if (returned) throw new InvalidOperationException("Broker returned an unroutable outbox message.");
                }
                finally { channel.BasicReturn -= OnReturn; }
                message.Status = OutboxMessageStatus.Published;
                message.PublishedAt = DateTimeOffset.UtcNow;
                message.LastError = null;
                message.NextAttemptAt = null;
            }
            catch (Exception ex)
            {
                publicationFailed = true;
                CloseBroker();
                Failures.Add(1);
                OutboxMessageRetry.MarkFailed(message, ex, _options.MaxPublishAttempts, _options.PublishRetryDelaySeconds);
                if (message.Status == OutboxMessageStatus.Abandoned)
                    logger.LogError(ex, "Outbox message {MessageId} exhausted retries and requires operator action.", message.MessageId);
                else logger.LogWarning(ex, "Outbox publication failed for {MessageId}.", message.MessageId);
                // Avoid opening one connection per remaining row during a broker outage.
                break;
            }
            finally { PublishTime.Record(watch.Elapsed.TotalMilliseconds); }
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var pending = db.OutboxMessages.Where(x => x.Status == OutboxMessageStatus.Pending || x.Status == OutboxMessageStatus.Failed);
        Backlog.Record(await pending.CountAsync(ct));
        var oldest = await pending.MinAsync(x => (DateTimeOffset?)x.OccurredAt, ct);
        if (oldest.HasValue) OldestAge.Record(Math.Max(0, (DateTimeOffset.UtcNow - oldest.Value).TotalSeconds));
        // Delete only confirmed publications, in bounded batches. Failed/abandoned rows remain available for replay.
        var cutoff = now.AddDays(-Math.Max(1, _options.PublishedRetentionDays));
        await db.OutboxMessages.Where(x => x.Status == OutboxMessageStatus.Published && x.PublishedAt < cutoff)
            .OrderBy(x => x.PublishedAt).Take(500).ExecuteDeleteAsync(ct);
        return !publicationFailed;
    }

    /// <summary>Reuses a healthy confirmed channel, recreating it after a broken connection.</summary>
    private IModel GetChannel()
    {
        if (_connection?.IsOpen == true && _channel?.IsOpen == true) return _channel;
        CloseBroker();
        _connection = connections.CreateConnection();
        _channel = _connection.CreateModel();
        topology.EnsureTopology(_channel);
        _channel.ConfirmSelect();
        return _channel;
    }

    /// <summary>Releases a broker session also when the worker is disposed before it starts.</summary>
    public override void Dispose()
    {
        CloseBroker();
        base.Dispose();
    }

    private void CloseBroker()
    {
        var channel = _channel;
        var connection = _connection;
        _channel = null;
        _connection = null;
        try { channel?.Dispose(); }
        catch (Exception ex) { logger.LogDebug(ex, "Failed to dispose a broken broker channel."); }
        try { connection?.Dispose(); }
        catch (Exception ex) { logger.LogDebug(ex, "Failed to dispose a broken broker connection."); }
    }
}
