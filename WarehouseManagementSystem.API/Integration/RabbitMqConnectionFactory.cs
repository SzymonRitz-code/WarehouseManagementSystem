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
            DispatchConsumersAsync = false
        };

        // TODO(RECRUITMENT): Po przejsciu na async consumer ustaw DispatchConsumersAsync=true i skonfiguruj
        // automatic recovery, heartbeat oraz client-provided connection name. Zerwane polaczenie powinno
        // byc wykrywalne, odnawialne i latwe do znalezienia w panelu RabbitMQ.

        return factory.CreateConnection();
    }
}
