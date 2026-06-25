using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Domain.Model.InventoryDomain;

public class Stock
{
    #region Properties and Fields

    public Guid Id { get; private set; }

    private decimal _quantityTotal;
    private decimal _quantityReserved;

    public decimal QuantityTotal => _quantityTotal;
    public decimal QuantityReserved => _quantityReserved;
    public decimal Available => _quantityTotal - _quantityReserved;

    public DateTimeOffset LastUpdated { get; private set; }

    public byte[] RowVersion { get; private set; }

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

    #endregion

    #region Constructors

    private Stock() { }

    public Stock(
        Guid productId,
        Guid warehouseId,
        Guid warehouseZoneId,
        Guid? productBatchId,
        decimal initialQuantity)
    {
        if (initialQuantity < 0)
        {
            throw new ArgumentException("Initial quantity cannot be negative.");
        }

        Id = Guid.NewGuid();
        ProductId = productId;
        WarehouseId = warehouseId;
        WarehouseZoneId = warehouseZoneId;
        ProductBatchId = productBatchId;

        _quantityTotal = initialQuantity;
        _quantityReserved = 0;

        LastUpdated = DateTimeOffset.UtcNow;
    }

    #endregion

    #region Quantity Operations

    public void Increase(decimal quantity)
    {
        ValidatePositive(quantity);

        _quantityTotal += quantity;

        Touch();
    }
    public void IncreaseReserved(decimal quantity)
    {
        ValidatePositive(quantity);

        _quantityReserved += quantity;

        Touch();
    }

    public void Decrease(decimal quantity)
    {
        ValidatePositive(quantity);

        if (quantity > Available)
        {
            throw new InvalidOperationException("Not enough available stock.");
        }

        _quantityTotal -= quantity;
        Touch();
    }
    public void DecreaseReserved(decimal quantity)
    {
        ValidatePositive(quantity);

        if (quantity > QuantityReserved)
        {
            throw new InvalidOperationException("Not enough reserved stock to decrease.");
        }

        _quantityReserved -= quantity;
        Touch();
    }

    public void AdjustTotal(decimal newTotal)
    {
        if (newTotal < 0)
        {
            throw new ArgumentException("Total quantity cannot be negative.");
        }

        if (newTotal < QuantityReserved)
        {
            throw new InvalidOperationException("Total cannot be lower than reserved quantity.");
        }

        _quantityTotal = newTotal;

        Touch();
    }

    #endregion

    #region Reservation Operations

    public StockReservation CreateReservation(
        decimal quantity,
        string source,
        UserSnapshot createdBy,
        DateTimeOffset? expiresAt = null)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity cannot be negative or zero.", nameof(quantity));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source must be provided.", nameof(source));
        }

        // Obliczamy ile jest dostępne na rezerwacje
        var availableToReserve = QuantityTotal - QuantityReserved;

        if (quantity > availableToReserve)
        {
            throw new InvalidOperationException("Not enough stock available to reserve.");
        }

        var reservation = new StockReservation(
            Id,
            quantity,
            source,
            createdBy,
            expiresAt);

        _reservations.Add(reservation);

        _quantityReserved += quantity;

        Touch();

        return reservation;
    }

    public void ReleaseReservation(Guid reservationId)
    {
        var reservation = GetReservation(reservationId);

        reservation.Release();

        _quantityReserved -= reservation.Quantity;

        Touch();
    }

    public void FulfillReservation(Guid reservationId)
    {
        var reservation = GetReservation(reservationId);

        reservation.Fulfill();

        _quantityReserved -= reservation.Quantity;
        _quantityTotal -= reservation.Quantity;

        Touch();
    }

    public void ConfirmReservation(Guid reservationId)
    {
        var reservation = GetReservation(reservationId);
        if (reservation == null)
        {
            throw new InvalidOperationException("Reservation not found.");
        }

        // zmniejszamy fizycznie ilość w magazynie
        //Decrease(reservation.Quantity);
        _quantityTotal -= reservation.Quantity;

        // zmniejszamy ilość zarezerwowaną
        //DecreaseReserved(reservation.Quantity);
        _quantityReserved -= reservation.Quantity;

        // ustawiamy rezerwację jako potwierdzoną
        reservation.Fulfill();

        _reservations.Remove(reservation);

        Touch();
    }

    public void CancelReservation(Guid reservationId)
    {
        var reservation = GetReservation(reservationId);

        reservation.Cancel();

        _quantityReserved -= reservation.Quantity;

        Touch();
    }

    public void ExpireReservation(Guid reservationId)
    {
        var reservation = GetReservation(reservationId);

        reservation.Expire();

        if (reservation.Status == Domain.Enums.ReservationStatus.Expired)
        {
            _quantityReserved -= reservation.Quantity;
            Touch();
        }
    }

    #endregion

    #region Helper Methods

    private StockReservation GetReservation(Guid reservationId)
    {
        var reservation = _reservations.FirstOrDefault(x => x.Id == reservationId);

        return reservation is null ? throw new InvalidOperationException("Reservation not found.") : reservation;
    }

    private void ValidatePositive(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.");
        }
    }
    public bool IsAvailable(decimal quantity)
    {
        return Available >= quantity;
    }
    private void Touch()
    {
        LastUpdated = DateTimeOffset.UtcNow;
    }

    #endregion
}
