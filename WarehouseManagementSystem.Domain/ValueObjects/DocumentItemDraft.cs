namespace WarehouseManagementSystem.Domain.ValueObjects;

public readonly record struct DocumentItemDraft
{
    public Guid ProductId { get; }
    public decimal Quantity { get; }
    public Guid? ProductBatchId { get; }
    public Guid? SourceZoneId { get; }
    public Guid? TargetZoneId { get; }

    public DocumentItemDraft(
        Guid productId,
        decimal quantity,
        Guid? productBatchId,
        Guid? sourceZoneId,
        Guid? targetZoneId)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("ProductId cannot be empty.");
        }

        if (productBatchId.HasValue && productBatchId == Guid.Empty)
        {
            throw new ArgumentException("ProductBatchId cannot be empty.");
        }

        if (sourceZoneId.HasValue && sourceZoneId == Guid.Empty)
        {
            throw new ArgumentException("SourceZoneId cannot be empty.");
        }

        if (targetZoneId.HasValue && targetZoneId == Guid.Empty)
        {
            throw new ArgumentException("TargetZoneId cannot be empty.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.");
        }

        ProductId = productId;
        Quantity = quantity;
        ProductBatchId = productBatchId;
        SourceZoneId = sourceZoneId;
        TargetZoneId = targetZoneId;
    }
}