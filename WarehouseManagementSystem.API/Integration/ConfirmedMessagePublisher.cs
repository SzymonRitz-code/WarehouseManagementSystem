using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace WarehouseManagementSystem.API.Integration;

/// <summary>Prevents acknowledging the source delivery before the broker accepts its retry copy.</summary>
public static class ConfirmedMessagePublisher
{


    /// <summary>
    /// Publishes a mandatory message and rejects negative confirms, timeouts and missing bindings.
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="exchange"></param>
    /// <param name="routingKey"></param>
    /// <param name="properties"></param>
    /// <param name="body"></param>
    /// <param name="timeout"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void Publish(IModel channel, string exchange, string routingKey, IBasicProperties properties,
        ReadOnlyMemory<byte> body, TimeSpan timeout)
    {
        var returned = false;
        void OnReturn(object? sender, BasicReturnEventArgs args) => returned = true;
        channel.ConfirmSelect();
        channel.BasicReturn += OnReturn;
        try
        {
            channel.BasicPublish(exchange, routingKey, true, properties, body);
            channel.WaitForConfirmsOrDie(timeout);
            if (returned) throw new InvalidOperationException("Broker returned an unroutable message.");
        }
        finally { channel.BasicReturn -= OnReturn; }
    }
}
