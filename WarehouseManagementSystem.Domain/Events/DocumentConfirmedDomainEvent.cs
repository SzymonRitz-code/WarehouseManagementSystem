using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Domain.Events;

/// <summary>
/// Raised when a warehouse document transitions from Draft to Confirmed status.
/// Carries a snapshot of the confirmed document for downstream consumers.
/// </summary>
public sealed class DocumentConfirmedDomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; }

    public Guid DocumentId { get; }
    public string DocumentNumber { get; }
    public string DocumentType { get; }
    public Guid SourceWarehouseId { get; }
    public Guid? TargetWarehouseId { get; }
    public UserSnapshot ConfirmedBy { get; }

    public DocumentConfirmedDomainEvent(
        Guid documentId,
        string documentNumber,
        string documentType,
        Guid sourceWarehouseId,
        Guid? targetWarehouseId,
        UserSnapshot confirmedBy,
        DateTimeOffset occurredAt)
    {
        DocumentId = documentId;
        DocumentNumber = documentNumber;
        DocumentType = documentType;
        SourceWarehouseId = sourceWarehouseId;
        TargetWarehouseId = targetWarehouseId;
        ConfirmedBy = confirmedBy;
        OccurredAt = occurredAt;
    }
}
