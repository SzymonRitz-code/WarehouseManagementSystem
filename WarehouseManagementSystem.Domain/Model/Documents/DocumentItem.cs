using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

namespace WarehouseManagementSystem.Domain.Model.DocumentsDomain;

public class DocumentItem
{
    private DocumentItem() { } // EF Core

    public DocumentItem(
        Guid productId,
        decimal quantity,
        Guid? productBatchId = null,
        Guid? sourceZoneId = null,
        Guid? targetZoneId = null)
    {
        Id = Guid.NewGuid();
        SetProduct(productId);
        SetQuantity(quantity);
        ProductBatchId = productBatchId;
        SourceZoneId = sourceZoneId;
        TargetZoneId = targetZoneId;
    }

    public Guid Id { get; private set; }
    public decimal Quantity { get; private set; }

    public Guid DocumentId { get; private set; }
    public Document Document { get; private set; }

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; }

    public Guid? ProductBatchId { get; private set; }
    public ProductBatch? ProductBatch { get; private set; }

    public Guid? SourceZoneId { get; private set; }
    public WarehouseZone? SourceZone { get; private set; }

    public Guid? TargetZoneId { get; private set; }
    public WarehouseZone? TargetZone { get; private set; }

    // ===== Business Methods =====

    public void SetQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        Quantity = quantity;
    }

    public void IncreaseQuantity(decimal value)
    {
        if (value <= 0)
            throw new ArgumentException("Increase value must be greater than zero.");

        Quantity += value;
    }

    public void DecreaseQuantity(decimal value)
    {
        if (value <= 0)
            throw new ArgumentException("Decrease value must be greater than zero.");

        if (Quantity - value <= 0)
            throw new InvalidOperationException("Quantity cannot be zero or negative.");

        Quantity -= value;
    }

    public void SetProduct(Guid productId)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty.");

        ProductId = productId;
    }

    public void AssignBatch(Guid? batchId)
    {
        ProductBatchId = batchId;
    }

    public void SetSourceZone(Guid? zoneId)
    {
        SourceZoneId = zoneId;
    }

    public void SetTargetZone(Guid? zoneId)
    {
        TargetZoneId = zoneId;
    }

    /// <summary>
    /// Walidacja logiki magazynowej zależnej od typu dokumentu.
    /// Możesz wywołać przed zatwierdzeniem dokumentu.
    /// </summary>
    public void ValidateForDocumentType(DocumentType type)
    {
        switch (type)
        {
            case DocumentType.PZ:
                if (TargetZoneId == null)
                    throw new InvalidOperationException("PZ requires target zone.");
                break;

            case DocumentType.WZ:
                if (SourceZoneId == null)
                    throw new InvalidOperationException("WZ requires source zone.");
                break;

            case DocumentType.MM:
                if (SourceZoneId == null || TargetZoneId == null)
                    throw new InvalidOperationException("MM requires both source and target zones.");
                break;

            case DocumentType.ADJ:
                // korekta – strefy opcjonalne
                break;
        }
    }
}
