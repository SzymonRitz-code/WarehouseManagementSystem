using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.Domain.Model.WarehouseDomain;

public class WarehouseZone
{
    public Guid Id { get; set; }
    public string Code { get; set; } // A1, B1, COLD
    public string Name { get; set; }
    public string TemperatureType { get; set; } // Ambient, Cold, Frozen
    public bool IsPickingZone { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; }

    public ICollection<Stock> Stocks { get; set; }
}
