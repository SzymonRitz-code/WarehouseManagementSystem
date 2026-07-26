namespace WarehouseManagementSystem.Contracts;

/// <summary>Stable integration contract published after a WMS document is confirmed.</summary>
public sealed class DocumentConfirmedIntegrationEvent
{
    public Guid MessageId { get; init; }
    public Guid CorrelationId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public Guid DocumentId { get; init; }
    public string DocumentNumber { get; init; } = null!;
    public string DocumentType { get; init; } = null!;
    public Guid SourceWarehouseId { get; init; }
    public Guid? TargetWarehouseId { get; init; }
    public DateTimeOffset ConfirmedAt { get; init; }
    public ConfirmedByPayload ConfirmedBy { get; init; } = null!;
    public IReadOnlyList<DocumentConfirmedItemPayload> Items { get; init; } = [];
}

public sealed class ConfirmedByPayload
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Email { get; init; } = null!;
}

public sealed class DocumentConfirmedItemPayload
{
    public Guid ProductId { get; init; }
    public decimal Quantity { get; init; }
    public Guid? ProductBatchId { get; init; }
    public Guid? SourceZoneId { get; init; }
    public Guid? TargetZoneId { get; init; }
}
