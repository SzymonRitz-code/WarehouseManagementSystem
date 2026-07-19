using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WarehouseManagementSystem.API.Integration.Contracts;

namespace WarehouseManagementSystem.FakeShipping;

public sealed class ShippingConsumerWorker(
    IServiceScopeFactory scopes,
    ShippingRabbitMqConnectionFactory connections,
    ShippingRabbitMqTopology topology,
    IOptions<ShippingMessagingOptions> options,
    ILogger<ShippingConsumerWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ShippingMessagingOptions _options = options.Value;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(() => Consume(stoppingToken), stoppingToken);

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
                scope.ServiceProvider.GetRequiredService<DocumentConfirmedHandler>().HandleAsync(message, ct).GetAwaiter().GetResult();
                channel.BasicAck(delivery.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "FakeShipping could not process delivery {DeliveryTag}", delivery.DeliveryTag);
                channel.BasicNack(delivery.DeliveryTag, false, requeue: false);
            }
        };
        channel.BasicConsume(_options.Queue, autoAck: false, consumer);
        ct.WaitHandle.WaitOne();
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
    public ushort PrefetchCount { get; set; } = 10;
}

public sealed class ShippingRabbitMqConnectionFactory(IOptions<ShippingMessagingOptions> options)
{
    public IConnection CreateConnection()
    {
        var o = options.Value;
        return new ConnectionFactory { HostName = o.HostName, Port = o.Port, UserName = o.UserName, Password = o.Password }.CreateConnection();
    }
}

public sealed class ShippingRabbitMqTopology(IOptions<ShippingMessagingOptions> options)
{
    public void EnsureTopology(IModel channel)
    {
        var o = options.Value;
        channel.ExchangeDeclare(o.Exchange, ExchangeType.Direct, durable: true);
        channel.QueueDeclare(o.Queue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(o.Queue, o.Exchange, o.RoutingKey);
    }
}
