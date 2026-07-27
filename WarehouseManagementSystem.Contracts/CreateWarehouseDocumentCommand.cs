namespace WarehouseManagementSystem.Contracts;

/// <summary>Command sent by ERP to request creation of a draft WMS document.</summary>
public sealed class CreateWarehouseDocumentCommand
{
    public Guid MessageId { get; init; }
    public Guid CorrelationId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public string ExternalOrderId { get; init; } = null!;
    public string DocumentType { get; init; } = null!;
    public Guid SourceWarehouseId { get; init; }
    public Guid? TargetWarehouseId { get; init; }
    public DateTime DocumentDate { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<CreateWarehouseDocumentItem> Items { get; init; } = [];
}

public sealed class CreateWarehouseDocumentItem
{
    public Guid ProductId { get; init; }
    public decimal Quantity { get; init; }
    public Guid? ProductBatchId { get; init; }
    public Guid? SourceZoneId { get; init; }
    public Guid? TargetZoneId { get; init; }
}
