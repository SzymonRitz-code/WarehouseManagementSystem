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
        // EXCHANGE: exchange przyjmuje publish od publishera i kieruje wiadomości do queue.
        channel.ExchangeDeclare(_options.Exchanges.WmsEvents, ExchangeType.Direct, durable: true);
        channel.ExchangeDeclare(_options.Exchanges.DeadLetter, ExchangeType.Direct, durable: true);
        channel.ExchangeDeclare(_options.Shipping.DocumentConfirmedRetryExchange, ExchangeType.Direct, durable: true);
        // Osobny DLX/DLQ daje miejsce na poison messages i analizę operacyjną zamiast cichego gubienia
        // błędnych wiadomości. To jest ważny element odpowiedzi na pytania o niezawodność integracji.

        // QUEUE: DLQ to konkretna kolejka wewnątrz brokera dla odrzuconych wiadomości.
        channel.QueueDeclare(
            queue: _options.Shipping.DocumentConfirmedDeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // ROUTING KEY: exchange używa tego klucza, aby skierować wiadomość do właściwej queue.
        channel.QueueBind(
            queue: _options.Shipping.DocumentConfirmedDeadLetterQueue,
            exchange: _options.Exchanges.DeadLetter,
            routingKey: _options.Shipping.DocumentConfirmedRoutingKey);

        channel.QueueDeclare(
            queue: _options.Shipping.DocumentConfirmedRetryQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-message-ttl"] = _options.Shipping.RetryDelaySeconds * 1000,
                ["x-dead-letter-exchange"] = _options.Exchanges.WmsEvents,
                ["x-dead-letter-routing-key"] = _options.Shipping.DocumentConfirmedRoutingKey
            });
        channel.QueueBind(
            queue: _options.Shipping.DocumentConfirmedRetryQueue,
            exchange: _options.Shipping.DocumentConfirmedRetryExchange,
            routingKey: _options.Shipping.DocumentConfirmedRoutingKey);

        // QUEUE: to główna kolejka, z której consumer będzie odbierał wiadomości.
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
        // Na tym etapie DLQ już jest, ale repo nie ma jeszcze osobnego procesu do replay/manual repair.
        // Jeśli na rozmowie padnie pytanie "jak odbudujesz stan po błędzie?", to właśnie tu naturalnie
        // dochodzi kolejny krok: narzędzie do ponownego puszczania wiadomości z DLQ.

        // EXCHANGE VS QUEUE: exchange kieruje wiadomość, a queue przechowuje ją do odbioru.
        channel.QueueBind(
            queue: _options.Shipping.DocumentConfirmedQueue,
            exchange: _options.Exchanges.WmsEvents,
            routingKey: _options.Shipping.DocumentConfirmedRoutingKey);

    }
}
