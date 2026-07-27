using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WarehouseManagementSystem.Contracts;

namespace WarehouseManagementSystem.FakeBilling;

public sealed class BillingConsumerWorker(
    IServiceScopeFactory scopeFactory, BillingRabbitMqConnectionFactory rabbitConnectionFactory, BillingRabbitMqTopology rabbitTopology,
    IOptions<BillingMessagingOptions> options, ILogger<BillingConsumerWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly BillingMessagingOptions _messagingOptions = options.Value;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => Consume(stoppingToken), stoppingToken);
    }

    private void Consume(CancellationToken ct)
    {
        using var rabbitConnection = rabbitConnectionFactory.CreateConnection();
        using var rabbitChannel = rabbitConnection.CreateModel();
        rabbitTopology.EnsureTopology(rabbitChannel);
        rabbitChannel.BasicQos(0, _messagingOptions.PrefetchCount, false);
        var billingConsumer = new EventingBasicConsumer(rabbitChannel);
        billingConsumer.Received += (_, delivery) =>
        {
            try
            {
                var documentConfirmedEvent = JsonSerializer.Deserialize<DocumentConfirmedIntegrationEvent>(Encoding.UTF8.GetString(delivery.Body.ToArray()), JsonOptions)
                    ?? throw new InvalidOperationException("DocumentConfirmed message is empty.");
                using var consumerScope = scopeFactory.CreateScope();
                consumerScope.ServiceProvider.GetRequiredService<DocumentConfirmedBillingHandler>().HandleAsync(documentConfirmedEvent, ct).GetAwaiter().GetResult();
                rabbitChannel.BasicAck(delivery.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                var completedRetries = BillingConsumerRetryPolicy.GetRetryCount(delivery.BasicProperties.Headers);
                var retryPolicy = new BillingConsumerRetryPolicy(_messagingOptions.MaxRetryAttempts);
                if (retryPolicy.ShouldRetry(completedRetries))
                {
                    var retryMessageProperties = CopyProperties(rabbitChannel, delivery.BasicProperties);
                    retryMessageProperties.Headers[BillingConsumerRetryPolicy.RetryCountHeader] = retryPolicy.NextRetryCount(completedRetries);
                    retryMessageProperties.Headers[BillingConsumerRetryPolicy.LastErrorHeader] = ex.Message;
                    retryMessageProperties.Headers[BillingConsumerRetryPolicy.LastAttemptAtHeader] = DateTimeOffset.UtcNow.ToString("O");
                    rabbitChannel.BasicPublish(_messagingOptions.RetryExchange, _messagingOptions.RoutingKey, retryMessageProperties, delivery.Body);
                    rabbitChannel.BasicAck(delivery.DeliveryTag, false);
                    logger.LogWarning(ex,
                        "FakeBilling decision retry {RetryCount}. MessageId {MessageId}, CorrelationId {CorrelationId}",
                        retryPolicy.NextRetryCount(completedRetries), delivery.BasicProperties.MessageId, delivery.BasicProperties.CorrelationId);
                    return;
                }

                logger.LogError(ex,
                    "FakeBilling decision dead-letter after {RetryCount} retries. MessageId {MessageId}, CorrelationId {CorrelationId}",
                    completedRetries, delivery.BasicProperties.MessageId, delivery.BasicProperties.CorrelationId);
                rabbitChannel.BasicNack(delivery.DeliveryTag, false, requeue: false);
            }
        };
        rabbitChannel.BasicConsume(_messagingOptions.Queue, autoAck: false, billingConsumer);
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
        var messagingOptions = options.Value;
        return new ConnectionFactory
        {
            HostName = messagingOptions.HostName,
            Port = messagingOptions.Port,
            UserName = messagingOptions.UserName,
            Password = messagingOptions.Password
        }.CreateConnection();
    }
}

public sealed class BillingRabbitMqTopology(IOptions<BillingMessagingOptions> options)
{
    public void EnsureTopology(IModel channel)
    {
        var messagingOptions = options.Value;
        channel.ExchangeDeclare(messagingOptions.Exchange, ExchangeType.Direct, durable: true);
        channel.ExchangeDeclare(messagingOptions.RetryExchange, ExchangeType.Direct, durable: true);
        channel.ExchangeDeclare(messagingOptions.DeadLetterExchange, ExchangeType.Direct, durable: true);
        channel.QueueDeclare(messagingOptions.DeadLetterQueue, true, false, false);
        channel.QueueBind(messagingOptions.DeadLetterQueue, messagingOptions.DeadLetterExchange, messagingOptions.RoutingKey);
        channel.QueueDeclare(messagingOptions.RetryQueue, true, false, false, new Dictionary<string, object>
        {
            ["x-message-ttl"] = messagingOptions.RetryDelaySeconds * 1000,
            ["x-dead-letter-exchange"] = messagingOptions.Exchange,
            ["x-dead-letter-routing-key"] = messagingOptions.RoutingKey
        });
        channel.QueueBind(messagingOptions.RetryQueue, messagingOptions.RetryExchange, messagingOptions.RoutingKey);
        channel.QueueDeclare(messagingOptions.Queue, true, false, false, new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = messagingOptions.DeadLetterExchange,
            ["x-dead-letter-routing-key"] = messagingOptions.RoutingKey
        });
        channel.QueueBind(messagingOptions.Queue, messagingOptions.Exchange, messagingOptions.RoutingKey);
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
