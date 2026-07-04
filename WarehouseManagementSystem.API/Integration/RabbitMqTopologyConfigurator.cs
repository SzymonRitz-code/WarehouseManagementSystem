using RabbitMQ.Client;
using WarehouseManagementSystem.API.Integration.Configuration;

namespace WarehouseManagementSystem.API.Integration;

/// <summary>
/// Represents a configurator for RabbitMQ topology, responsible for ensuring the necessary exchanges and queues are declared and bound.
/// </summary>
public interface IRabbitMqTopologyConfigurator
{
    /// <summary>
    /// Ensures that the necessary RabbitMQ topology (exchanges, queues, and bindings) is declared on the provided channel.
    /// </summary>
    /// <param name="channel">The RabbitMQ channel on which to declare the topology.</param>
    void EnsureTopology(IModel channel);
}

public class RabbitMqTopologyConfigurator : IRabbitMqTopologyConfigurator
{
    private readonly MessagingOptions _options;

    public RabbitMqTopologyConfigurator(Microsoft.Extensions.Options.IOptions<MessagingOptions> options)
    {
        _options = options.Value;
    }

    public void EnsureTopology(IModel channel)
    {
        channel.ExchangeDeclare(_options.Exchanges.WmsEvents, ExchangeType.Direct, durable: true);
        channel.ExchangeDeclare(_options.Exchanges.DeadLetter, ExchangeType.Direct, durable: true);

        channel.QueueDeclare(
            queue: _options.Shipping.DocumentConfirmedDeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueBind(
            queue: _options.Shipping.DocumentConfirmedDeadLetterQueue,
            exchange: _options.Exchanges.DeadLetter,
            routingKey: _options.Shipping.DocumentConfirmedRoutingKey);

        channel.QueueDeclare(
            queue: _options.Shipping.DocumentConfirmedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = _options.Exchanges.DeadLetter,
                ["x-dead-letter-routing-key"] = _options.Shipping.DocumentConfirmedRoutingKey
            });

        channel.QueueBind(
            queue: _options.Shipping.DocumentConfirmedQueue,
            exchange: _options.Exchanges.WmsEvents,
            routingKey: _options.Shipping.DocumentConfirmedRoutingKey);
    }
}
