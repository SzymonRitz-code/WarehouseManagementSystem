using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

namespace WarehouseManagementSystem.Domain.Model.DocumentsDomain;

public class DocumentItem
{
    public Guid Id { get; set; }
    public decimal Quantity { get; set; }


    public Guid DocumentId { get; set; }
    public virtual Document Document { get; set; }

    public Guid ProductId { get; set; }
    public virtual Product Product { get; set; }

    public Guid? ProductBatchId { get; set; }
    public virtual ProductBatch? ProductBatch { get; set; }

    public Guid? SourceZoneId { get; set; }
    public virtual WarehouseZone? SourceZone { get; set; }

    public Guid? TargetZoneId { get; set; }
    public virtual WarehouseZone? TargetZone { get; set; }
}
