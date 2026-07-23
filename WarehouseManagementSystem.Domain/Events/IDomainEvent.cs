namespace WarehouseManagementSystem.Domain.Events;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something significant that happened within the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// UTC timestamp when the event occurred.
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
