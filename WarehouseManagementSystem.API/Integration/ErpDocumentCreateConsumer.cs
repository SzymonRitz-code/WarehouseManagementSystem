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
    protected override Task ExecuteAsync(CancellationToken ct)
    {
        return _options.Enabled
            ? Task.Run(() => Consume(ct), ct)
            : Task.CompletedTask;
    }

    private void Consume(CancellationToken ct)
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
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (_, delivery) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<CreateWarehouseDocumentCommand>(Encoding.UTF8.GetString(delivery.Body.ToArray()), Json) ?? throw new PermanentIntegrationException("Empty ERP command.");
                using var scope = scopes.CreateScope(); scope.ServiceProvider.GetRequiredService<ErpDocumentCreateHandler>().HandleAsync(message, ct).GetAwaiter().GetResult();
                channel.BasicAck(delivery.DeliveryTag, false);
            }
            catch (PermanentIntegrationException ex) { logger.LogError(ex, "Permanent ERP command failure; sending {MessageId} to DLQ", delivery.BasicProperties.MessageId); channel.BasicNack(delivery.DeliveryTag, false, false); }
            catch (Exception ex)
            {
                var retries = ConsumerRetryPolicy.GetRetryCount(delivery.BasicProperties.Headers); var policy = new ConsumerRetryPolicy(o.MaxRetryAttempts);
                if (policy.ShouldRetry(retries)) { var p = Copy(channel, delivery.BasicProperties); p.Headers[ConsumerRetryPolicy.RetryCountHeader] = policy.NextRetryCount(retries); p.Headers[ConsumerRetryPolicy.LastErrorHeader] = ex.Message; channel.BasicPublish(o.RetryExchange, o.RoutingKey, p, delivery.Body); channel.BasicAck(delivery.DeliveryTag, false); }
                else { logger.LogError(ex, "ERP command exhausted retries; sending {MessageId} to DLQ", delivery.BasicProperties.MessageId); channel.BasicNack(delivery.DeliveryTag, false, false); }
            }
        };
        channel.BasicConsume(o.Queue, false, consumer); ct.WaitHandle.WaitOne();
    }
    private static IBasicProperties Copy(IModel c, IBasicProperties s) { var p = c.CreateBasicProperties(); p.Persistent = true; p.MessageId = s.MessageId; p.CorrelationId = s.CorrelationId; p.Type = s.Type; p.Timestamp = s.Timestamp; p.Headers = s.Headers is null ? new Dictionary<string, object>() : new Dictionary<string, object>(s.Headers); return p; }
}
