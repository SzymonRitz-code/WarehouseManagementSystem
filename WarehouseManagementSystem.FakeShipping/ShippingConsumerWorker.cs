using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WarehouseManagementSystem.Contracts;

namespace WarehouseManagementSystem.FakeShipping;

public sealed class ShippingConsumerWorker(
    IServiceScopeFactory scopeFactory,
    ShippingRabbitMqConnectionFactory rabbitConnectionFactory,
    ShippingRabbitMqTopology rabbitTopology,
    IOptions<ShippingMessagingOptions> options,
    ILogger<ShippingConsumerWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ShippingMessagingOptions _messagingOptions = options.Value;

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
        var shippingConsumer = new EventingBasicConsumer(rabbitChannel);
        shippingConsumer.Received += (_, delivery) =>
        {
            try
            {
                var documentConfirmedEvent = JsonSerializer.Deserialize<DocumentConfirmedIntegrationEvent>(Encoding.UTF8.GetString(delivery.Body.ToArray()), JsonOptions)
                    ?? throw new InvalidOperationException("DocumentConfirmed message is empty.");
                using var consumerScope = scopeFactory.CreateScope();
                consumerScope.ServiceProvider.GetRequiredService<DocumentConfirmedHandler>().HandleAsync(documentConfirmedEvent, ct).GetAwaiter().GetResult();
                rabbitChannel.BasicAck(delivery.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                var completedRetries = ShippingConsumerRetryPolicy.GetRetryCount(delivery.BasicProperties.Headers);
                var retryPolicy = new ShippingConsumerRetryPolicy(_messagingOptions.MaxRetryAttempts);

                if (retryPolicy.ShouldRetry(completedRetries))
                {
                    var retryMessageProperties = CopyProperties(rabbitChannel, delivery.BasicProperties);
                    retryMessageProperties.Headers[ShippingConsumerRetryPolicy.RetryCountHeader] = retryPolicy.NextRetryCount(completedRetries);
                    retryMessageProperties.Headers[ShippingConsumerRetryPolicy.LastErrorHeader] = ex.Message;
                    retryMessageProperties.Headers[ShippingConsumerRetryPolicy.LastAttemptAtHeader] = DateTimeOffset.UtcNow.ToString("O");

                    rabbitChannel.BasicPublish(_messagingOptions.RetryExchange, _messagingOptions.RoutingKey, retryMessageProperties, delivery.Body);
                    rabbitChannel.BasicAck(delivery.DeliveryTag, false);

                    logger.LogWarning(ex,
                        "FakeShipping scheduled retry {RetryCount} for MessageId {MessageId}",
                        retryPolicy.NextRetryCount(completedRetries), delivery.BasicProperties.MessageId);

                    return;
                }

                logger.LogError(ex,
                    "FakeShipping sent MessageId {MessageId} to DLQ after {RetryCount} retries",
                    delivery.BasicProperties.MessageId, completedRetries);

                rabbitChannel.BasicNack(delivery.DeliveryTag, false, requeue: false);
            }
        };
        rabbitChannel.BasicConsume(_messagingOptions.Queue, autoAck: false, shippingConsumer);
        ct.WaitHandle.WaitOne();
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

public sealed class ShippingMessagingOptions
{
    public const string SectionName = "ShippingMessaging";
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "wms.events";
    public string Queue { get; set; } = "shipping.document-confirmed";
    public string RoutingKey { get; set; } = "document.confirmed";
    public string RetryExchange { get; set; } = "wms.events.retry";
    public string RetryQueue { get; set; } = "shipping.document-confirmed.retry";
    public string DeadLetterExchange { get; set; } = "wms.events.dlx";
    public string DeadLetterQueue { get; set; } = "shipping.document-confirmed.dlq";
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 10;
    public ushort PrefetchCount { get; set; } = 10;
}

public sealed class ShippingRabbitMqConnectionFactory(IOptions<ShippingMessagingOptions> options)
{
    public IConnection CreateConnection()
    {
        var messagingOptions = options.Value;
        return new ConnectionFactory { HostName = messagingOptions.HostName, Port = messagingOptions.Port, UserName = messagingOptions.UserName, Password = messagingOptions.Password }.CreateConnection();
    }
}

public sealed class ShippingRabbitMqTopology(IOptions<ShippingMessagingOptions> options)
{
    public void EnsureTopology(IModel channel)
    {
        var messagingOptions = options.Value;
        channel.ExchangeDeclare(messagingOptions.Exchange, ExchangeType.Direct, durable: true);
        channel.ExchangeDeclare(messagingOptions.RetryExchange, ExchangeType.Direct, durable: true);
        channel.ExchangeDeclare(messagingOptions.DeadLetterExchange, ExchangeType.Direct, durable: true);
        channel.QueueDeclare(messagingOptions.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(messagingOptions.DeadLetterQueue, messagingOptions.DeadLetterExchange, messagingOptions.RoutingKey);
        channel.QueueDeclare(messagingOptions.RetryQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-message-ttl"] = messagingOptions.RetryDelaySeconds * 1000,
                ["x-dead-letter-exchange"] = messagingOptions.Exchange,
                ["x-dead-letter-routing-key"] = messagingOptions.RoutingKey
            });
        channel.QueueBind(messagingOptions.RetryQueue, messagingOptions.RetryExchange, messagingOptions.RoutingKey);
        channel.QueueDeclare(messagingOptions.Queue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = messagingOptions.DeadLetterExchange,
                ["x-dead-letter-routing-key"] = messagingOptions.RoutingKey
            });
        channel.QueueBind(messagingOptions.Queue, messagingOptions.Exchange, messagingOptions.RoutingKey);
    }
}
