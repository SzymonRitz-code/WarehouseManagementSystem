using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

namespace WarehouseManagementSystem.Domain.Model.InventoryDomain;
public class Stock
{
    public Guid Id { get; private set; }

    private decimal _quantityTotal;
    private decimal _quantityReserved;

    public decimal QuantityTotal => _quantityTotal;
    public decimal QuantityReserved => _quantityReserved;

    public decimal Available => _quantityTotal - _quantityReserved;

    public DateTimeOffset LastUpdated { get; private set; }

    public byte[]? RowVersion { get; private set; }

    // FK
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid WarehouseZoneId { get; private set; }
    public Guid? ProductBatchId { get; private set; }

    // Navigation
    public virtual Product Product { get; private set; }
    public virtual Warehouse Warehouse { get; private set; }
    public virtual WarehouseZone WarehouseZone { get; private set; }
    public virtual ProductBatch? ProductBatch { get; private set; }

    private readonly List<StockReservation> _reservations = new();
    public IReadOnlyCollection<StockReservation> Reservations => _reservations;

    // EF Core constructor
    private Stock() { }

    public Stock(
        Guid productId,
        Guid warehouseId,
        Guid warehouseZoneId,
        Guid? productBatchId,
        decimal initialQuantity)
    {
        if (initialQuantity < 0)
            throw new ArgumentException("Initial quantity cannot be negative.");

        Id = Guid.NewGuid();
        ProductId = productId;
        WarehouseId = warehouseId;
        WarehouseZoneId = warehouseZoneId;
        ProductBatchId = productBatchId;

        _quantityTotal = initialQuantity;
        _quantityReserved = 0;

        LastUpdated = DateTimeOffset.UtcNow;
    }

    // ========================
    // DOMAIN OPERATIONS
    // ========================

    public void Increase(decimal quantity)
    {
        ValidatePositive(quantity);

        _quantityTotal += quantity;
        Touch();
    }

    public void Decrease(decimal quantity)
    {
        ValidatePositive(quantity);

        if (quantity > Available)
            throw new ArgumentException("Not enough available stock.");

        _quantityTotal -= quantity;
        Touch();
    }

    public void Reserve(decimal quantity)
    {
        ValidatePositive(quantity);

        if (quantity > Available)
            throw new ArgumentException("Not enough stock available to reserve.");

        _quantityReserved += quantity;
        Touch();
    }

    public void ReleaseReservation(decimal quantity)
    {
        ValidatePositive(quantity);

        if (quantity > _quantityReserved)
            throw new ArgumentException("Cannot release more than reserved.");

        _quantityReserved -= quantity;
        Touch();
    }

    public void AdjustTotal(decimal newTotal)
    {
        if (newTotal < 0)
            throw new ArgumentException("Total quantity cannot be negative.");

        if (newTotal < _quantityReserved)
            throw new ArgumentException("Total cannot be lower than reserved quantity.");

        _quantityTotal = newTotal;
        Touch();
    }

    private void ValidatePositive(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");
    }

    private void Touch()
    {
        LastUpdated = DateTimeOffset.UtcNow;
    }
}
