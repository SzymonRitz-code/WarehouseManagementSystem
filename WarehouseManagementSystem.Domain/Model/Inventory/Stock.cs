using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

namespace WarehouseManagementSystem.Domain.Model.InventoryDomain;
public class Stock
{
    public Guid Id { get; set; }

    public decimal QuantityTotal { get; set; }
    public decimal QuantityReserved { get; set; }
    public byte[]? RowVersion { get; set; }
    public DateTimeOffset LastUpdated { get; set; }

    public decimal Available => QuantityTotal - QuantityReserved;


    public Guid ProductId { get; set; }
    public virtual Product Product { get; set; }

    public Guid WarehouseId { get; set; }
    public virtual Warehouse Warehouse { get; set; }

    public Guid WarehouseZoneId { get; set; }
    public virtual WarehouseZone WarehouseZone { get; set; }

    public Guid? ProductBatchId { get; set; }
    public virtual ProductBatch? ProductBatch { get; set; }

    public virtual ICollection<StockReservation> Reservations { get; set; }
}
