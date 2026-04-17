using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.Domain.Model.WarehouseDomain;

public class WarehouseZone
{
    private WarehouseZone() { } // EF

    public WarehouseZone(
        string code,
        string name,
        TemperatureType temperatureType,
        bool isPickingZone,
        Guid warehouseId)
    {
        Id = Guid.NewGuid();
        SetCode(code);
        SetName(name);

        TemperatureType = temperatureType;
        IsPickingZone = isPickingZone;
        WarehouseId = warehouseId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public TemperatureType TemperatureType { get; private set; }
    public bool IsPickingZone { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Guid WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; }

    public ICollection<Stock> Stocks { get; private set; } = new List<Stock>();

    public void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Zone code cannot be empty.");

        Code = code.Trim().ToUpperInvariant();
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Zone name cannot be empty.");

        Name = name.Trim();
    }
    public void SetTemperatureType(TemperatureType temperatureType)
    {
        TemperatureType = temperatureType;
    }
    public void SetWarehouse(Guid warehouseId)
    {
        WarehouseId = warehouseId;
    }
    public void SetPickingZone(bool value) => IsPickingZone = value;

    public bool ContainsStock() => Stocks.Any();

    public void EnsureCanBeRemoved()
    {
        if (ContainsStock())
            throw new InvalidOperationException(
                "Zone cannot be removed because it contains stock.");
    }
}
