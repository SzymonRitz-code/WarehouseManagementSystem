namespace WarehouseManagementSystem.API.Integration;

/// <summary>
/// Represents an outbox for integration messages.
/// </summary>
public interface IIntegrationOutbox
{
    /// <summary>
    /// Adds a message to the outbox for later processing.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="messageId">The unique identifier of the message.</param>
    /// <param name="correlationId">The correlation identifier for the message.</param>
    /// <param name="routingKey">The routing key for the message.</param>
    /// <param name="message">The message to be added to the outbox.</param>
    /// <param name="occurredAt">The timestamp when the message occurred.</param>
    void Add<TMessage>(Guid messageId, Guid? correlationId, string routingKey, TMessage message, DateTimeOffset occurredAt);
}
