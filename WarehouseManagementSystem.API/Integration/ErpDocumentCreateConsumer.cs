using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WarehouseManagementSystem.API.Integration.Configuration;
using WarehouseManagementSystem.Contracts;

namespace WarehouseManagementSystem.API.Integration;

public sealed class ErpDocumentCreateConsumer(IServiceScopeFactory scopes, IRabbitMqConnectionFactory connections, IOptions<MessagingOptions> options, ILogger<ErpDocumentCreateConsumer> logger) : BackgroundService
{
    private readonly MessagingOptions _options = options.Value;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    /// <summary>Starts asynchronous consumption and retries initial connection failures.</summary>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_options.Enabled) return;
        while (!ct.IsCancellationRequested)
        {
            try { await Consume(ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "ERP consumer connection failed; retrying."); }
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }

    /// <summary>Commits an ERP command before ACK and confirms retry copies before acknowledging their source.</summary>
    private async Task Consume(CancellationToken ct)
    {
        using var connection = connections.CreateConnection(); using var channel = connection.CreateModel();
        var o = _options.Erp;
        channel.ExchangeDeclare(o.CommandsExchange, ExchangeType.Direct, durable: true);
        channel.ExchangeDeclare(o.RetryExchange, ExchangeType.Direct, durable: true);
        channel.ExchangeDeclare(o.DeadLetterExchange, ExchangeType.Direct, durable: true);
        channel.QueueDeclare(o.DeadLetterQueue, true, false, false); channel.QueueBind(o.DeadLetterQueue, o.DeadLetterExchange, o.RoutingKey);
        channel.QueueDeclare(o.RetryQueue, true, false, false, new Dictionary<string, object> { ["x-message-ttl"] = o.RetryDelaySeconds * 1000, ["x-dead-letter-exchange"] = o.CommandsExchange, ["x-dead-letter-routing-key"] = o.RoutingKey }); channel.QueueBind(o.RetryQueue, o.RetryExchange, o.RoutingKey);
        channel.QueueDeclare(o.Queue, true, false, false, new Dictionary<string, object> { ["x-dead-letter-exchange"] = o.DeadLetterExchange, ["x-dead-letter-routing-key"] = o.RoutingKey }); channel.QueueBind(o.Queue, o.CommandsExchange, o.RoutingKey);
        channel.BasicQos(0, o.PrefetchCount, false);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, delivery) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<CreateWarehouseDocumentCommand>(Encoding.UTF8.GetString(delivery.Body.ToArray()), Json) ?? throw new PermanentIntegrationException("Empty ERP command.");
                using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<ErpDocumentCreateHandler>().HandleAsync(message, ct);
                channel.BasicAck(delivery.DeliveryTag, false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (JsonException ex) { logger.LogError(ex, "Invalid ERP JSON; sending to DLQ."); channel.BasicNack(delivery.DeliveryTag, false, false); }
            catch (PermanentIntegrationException ex) { logger.LogError(ex, "Permanent ERP command failure; sending {MessageId} to DLQ", delivery.BasicProperties.MessageId); channel.BasicNack(delivery.DeliveryTag, false, false); }
            catch (Exception ex)
            {
                var retries = ConsumerRetryPolicy.GetRetryCount(delivery.BasicProperties.Headers); var policy = new ConsumerRetryPolicy(o.MaxRetryAttempts);
                if (policy.ShouldRetry(retries)) { var p = Copy(channel, delivery.BasicProperties); p.Headers[ConsumerRetryPolicy.RetryCountHeader] = policy.NextRetryCount(retries); p.Headers[ConsumerRetryPolicy.LastErrorHeader] = ex.Message; try { ConfirmedMessagePublisher.Publish(channel, o.RetryExchange, o.RoutingKey, p, delivery.Body, TimeSpan.FromSeconds(_options.PublishConfirmTimeoutSeconds)); }
                    catch (Exception publishError) { logger.LogError(publishError, "ERP retry publication failed."); if (channel.IsOpen) channel.BasicNack(delivery.DeliveryTag, false, true); return; }
                    channel.BasicAck(delivery.DeliveryTag, false); }
                else { logger.LogError(ex, "ERP command exhausted retries; sending {MessageId} to DLQ", delivery.BasicProperties.MessageId); channel.BasicNack(delivery.DeliveryTag, false, false); }
            }
        };
        channel.BasicConsume(o.Queue, false, consumer); await Task.Delay(Timeout.Infinite, ct);
    }
    private static IBasicProperties Copy(IModel c, IBasicProperties s) { var p = c.CreateBasicProperties(); p.Persistent = true; p.MessageId = s.MessageId; p.CorrelationId = s.CorrelationId; p.Type = s.Type; p.Timestamp = s.Timestamp; p.Headers = s.Headers is null ? new Dictionary<string, object>() : new Dictionary<string, object>(s.Headers); return p; }
}
