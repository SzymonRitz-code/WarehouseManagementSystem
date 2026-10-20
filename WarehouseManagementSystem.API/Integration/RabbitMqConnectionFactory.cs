using RabbitMQ.Client;
using WarehouseManagementSystem.API.Integration.Configuration;

namespace WarehouseManagementSystem.API.Integration;

/// <summary>
/// Represents a factory for creating RabbitMQ connections.
/// </summary>
public interface IRabbitMqConnectionFactory
{
    /// <summary>
    /// Creates a new RabbitMQ connection.
    /// </summary>
    /// <returns>A new instance of <see cref="IConnection"/>.</returns>
    IConnection CreateConnection();
}

public class RabbitMqConnectionFactory : IRabbitMqConnectionFactory
{
    private readonly MessagingOptions _options;

    public RabbitMqConnectionFactory(Microsoft.Extensions.Options.IOptions<MessagingOptions> options)
    {
        _options = options.Value;
    }

    public IConnection CreateConnection()
    {
        // BROKER: RabbitMQ jest tutaj konkretnym brokerem, czyli infrastrukturą transportową.
        var factory = new ConnectionFactory
        {
            HostName = _options.RabbitMq.HostName,
            Port = _options.RabbitMq.Port,
            UserName = _options.RabbitMq.UserName,
            Password = _options.RabbitMq.Password,
            VirtualHost = _options.RabbitMq.VirtualHost,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            RequestedHeartbeat = TimeSpan.FromSeconds(30),
            ClientProvidedName = "wms-api"
        };


        return factory.CreateConnection();
    }
}
