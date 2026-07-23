using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Domain.Events;

/// <summary>
/// Raised when a warehouse document is cancelled.
/// </summary>
public sealed class DocumentCancelledDomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; }

    public Guid DocumentId { get; }
    public UserSnapshot CancelledBy { get; }

    public DocumentCancelledDomainEvent(
        Guid documentId,
        UserSnapshot cancelledBy,
        DateTimeOffset occurredAt)
    {
        DocumentId = documentId;
        CancelledBy = cancelledBy;
        OccurredAt = occurredAt;
    }
}
