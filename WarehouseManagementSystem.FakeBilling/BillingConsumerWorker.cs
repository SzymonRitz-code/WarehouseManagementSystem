using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WarehouseManagementSystem.Contracts;

namespace WarehouseManagementSystem.FakeBilling;

public sealed class BillingConsumerWorker(
    IServiceScopeFactory scopes, BillingRabbitMqConnectionFactory connections, BillingRabbitMqTopology topology,
    IOptions<BillingMessagingOptions> options, ILogger<BillingConsumerWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly BillingMessagingOptions _options = options.Value;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => Consume(stoppingToken), stoppingToken);
    }

    private void Consume(CancellationToken ct)
    {
        using var connection = connections.CreateConnection();
        using var channel = connection.CreateModel();
        topology.EnsureTopology(channel);
        channel.BasicQos(0, _options.PrefetchCount, false);
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (_, delivery) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<DocumentConfirmedIntegrationEvent>(Encoding.UTF8.GetString(delivery.Body.ToArray()), JsonOptions)
                    ?? throw new InvalidOperationException("DocumentConfirmed message is empty.");
                using var scope = scopes.CreateScope();
                scope.ServiceProvider.GetRequiredService<DocumentConfirmedBillingHandler>().HandleAsync(message, ct).GetAwaiter().GetResult();
                channel.BasicAck(delivery.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                var retries = BillingConsumerRetryPolicy.GetRetryCount(delivery.BasicProperties.Headers);
                var policy = new BillingConsumerRetryPolicy(_options.MaxRetryAttempts);
                if (policy.ShouldRetry(retries))
                {
                    var properties = CopyProperties(channel, delivery.BasicProperties);
                    properties.Headers[BillingConsumerRetryPolicy.RetryCountHeader] = policy.NextRetryCount(retries);
                    properties.Headers[BillingConsumerRetryPolicy.LastErrorHeader] = ex.Message;
                    properties.Headers[BillingConsumerRetryPolicy.LastAttemptAtHeader] = DateTimeOffset.UtcNow.ToString("O");
                    channel.BasicPublish(_options.RetryExchange, _options.RoutingKey, properties, delivery.Body);
                    channel.BasicAck(delivery.DeliveryTag, false);
                    logger.LogWarning(ex,
                        "FakeBilling decision retry {RetryCount}. MessageId {MessageId}, CorrelationId {CorrelationId}",
                        policy.NextRetryCount(retries), delivery.BasicProperties.MessageId, delivery.BasicProperties.CorrelationId);
                    return;
                }

                logger.LogError(ex,
                    "FakeBilling decision dead-letter after {RetryCount} retries. MessageId {MessageId}, CorrelationId {CorrelationId}",
                    retries, delivery.BasicProperties.MessageId, delivery.BasicProperties.CorrelationId);
                channel.BasicNack(delivery.DeliveryTag, false, requeue: false);
            }
        };
        channel.BasicConsume(_options.Queue, autoAck: false, consumer);
        ct.WaitHandle.WaitOne();
    }

    private static IBasicProperties CopyProperties(IModel channel, IBasicProperties source)
    {
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true; properties.MessageId = source.MessageId; properties.Type = source.Type;
        properties.CorrelationId = source.CorrelationId; properties.Timestamp = source.Timestamp;
        properties.Headers = source.Headers is null ? new Dictionary<string, object>() : new Dictionary<string, object>(source.Headers);
        return properties;
    }
}

public sealed class BillingMessagingOptions
{
    public const string SectionName = "BillingMessaging";
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "wms.events";
    public string Queue { get; set; } = "billing.document-confirmed";
    public string RoutingKey { get; set; } = "document.confirmed";
    public string RetryExchange { get; set; } = "wms.events.retry";
    public string RetryQueue { get; set; } = "billing.document-confirmed.retry";
    public string DeadLetterExchange { get; set; } = "wms.events.dlx";
    public string DeadLetterQueue { get; set; } = "billing.document-confirmed.dlq";
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 10;
    public ushort PrefetchCount { get; set; } = 10;
}

public sealed class BillingRabbitMqConnectionFactory(IOptions<BillingMessagingOptions> options)
{
    public IConnection CreateConnection()
    {
        var o = options.Value;
        return new ConnectionFactory
        {
            HostName = o.HostName,
            Port = o.Port,
            UserName = o.UserName,
            Password = o.Password
        }.CreateConnection();
    }
}

public sealed class BillingRabbitMqTopology(IOptions<BillingMessagingOptions> options)
{
    public void EnsureTopology(IModel channel)
    {
        var o = options.Value;
        channel.ExchangeDeclare(o.Exchange, ExchangeType.Direct, durable: true);
        channel.ExchangeDeclare(o.RetryExchange, ExchangeType.Direct, durable: true);
        channel.ExchangeDeclare(o.DeadLetterExchange, ExchangeType.Direct, durable: true);
        channel.QueueDeclare(o.DeadLetterQueue, true, false, false);
        channel.QueueBind(o.DeadLetterQueue, o.DeadLetterExchange, o.RoutingKey);
        channel.QueueDeclare(o.RetryQueue, true, false, false, new Dictionary<string, object>
        {
            ["x-message-ttl"] = o.RetryDelaySeconds * 1000,
            ["x-dead-letter-exchange"] = o.Exchange,
            ["x-dead-letter-routing-key"] = o.RoutingKey
        });
        channel.QueueBind(o.RetryQueue, o.RetryExchange, o.RoutingKey);
        channel.QueueDeclare(o.Queue, true, false, false, new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = o.DeadLetterExchange,
            ["x-dead-letter-routing-key"] = o.RoutingKey
        });
        channel.QueueBind(o.Queue, o.Exchange, o.RoutingKey);
    }
}

public sealed class BillingConsumerRetryPolicy
{
    public const string RetryCountHeader = "x-wms-retry-count";
    public const string LastErrorHeader = "x-wms-last-error";
    public const string LastAttemptAtHeader = "x-wms-last-attempt-at";
    private readonly int _maxRetryAttempts;
    public BillingConsumerRetryPolicy(int maxRetryAttempts)
    {
        _maxRetryAttempts = Math.Max(0, maxRetryAttempts);
    }

    public bool ShouldRetry(int completedRetries)
    {
        return completedRetries < _maxRetryAttempts;
    }

    public int NextRetryCount(int completedRetries)
    {
        return completedRetries + 1;
    }

    public static int GetRetryCount(IDictionary<string, object>? headers)
    {
        return headers is not null && headers.TryGetValue(RetryCountHeader, out var value) && value is not null
            ? Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)
            : 0;
    }
}
