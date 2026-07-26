using RabbitMQ.Client;
using WarehouseManagementSystem.API.Integration.Configuration;

namespace WarehouseManagementSystem.API.Integration;

public interface IRabbitMqTopologyConfigurator
{
    void EnsureTopology(IModel channel);
}

/// <summary>Publisher-owned RabbitMQ topology. Consumer queues are declared by their owning processes.</summary>
public sealed class RabbitMqTopologyConfigurator(Microsoft.Extensions.Options.IOptions<MessagingOptions> options) : IRabbitMqTopologyConfigurator
{
    private readonly MessagingOptions _options = options.Value;

    public void EnsureTopology(IModel channel)
    {
        channel.ExchangeDeclare(_options.Exchanges.WmsEvents, ExchangeType.Direct, durable: true);
        channel.ExchangeDeclare(_options.Exchanges.DeadLetter, ExchangeType.Direct, durable: true);
    }
}
