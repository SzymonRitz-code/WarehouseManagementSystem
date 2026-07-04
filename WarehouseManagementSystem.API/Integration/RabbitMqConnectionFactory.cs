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
        var factory = new ConnectionFactory
        {
            HostName = _options.RabbitMq.HostName,
            Port = _options.RabbitMq.Port,
            UserName = _options.RabbitMq.UserName,
            Password = _options.RabbitMq.Password,
            VirtualHost = _options.RabbitMq.VirtualHost,
            DispatchConsumersAsync = false
        };

        return factory.CreateConnection();
    }
}
