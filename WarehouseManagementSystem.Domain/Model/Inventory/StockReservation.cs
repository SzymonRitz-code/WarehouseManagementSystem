using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Domain.Model.InventoryDomain;
public class StockReservation
{
    public Guid Id { get; private set; }

    private decimal _quantity;
    public decimal Quantity => _quantity;

    private string _reservationSource = null!;
    public string ReservationSource => _reservationSource;

    public ReservationStatus Status { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public UserSnapshot CreatedByUser { get; private set; }

    public Guid StockId { get; private set; }
    public Stock Stock { get; private set; }

    private StockReservation() { }

    public StockReservation(
        Guid stockId,
        decimal quantity,
        string reservationSource,
        UserSnapshot createdByUser,
        DateTimeOffset? expiresAt = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Reservation quantity must be greater than zero.");

        Id = Guid.NewGuid();
        StockId = stockId;

        SetReservationSource(reservationSource);

        _quantity = quantity;

        CreatedAt = DateTimeOffset.UtcNow;
        CreatedByUser = createdByUser;

        SetExpiration(expiresAt);

        Status = ReservationStatus.Active;
    }

    public void SetReservationSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Reservation source is required.");

        if (source.Length > 50)
            throw new ArgumentException("Reservation source too long.");

        _reservationSource = source.Trim();
    }

    public void SetExpiration(DateTimeOffset? expiresAt)
    {
        if (expiresAt.HasValue && expiresAt <= CreatedAt)
            throw new ArgumentException("Expiration must be later than creation time.");

        ExpiresAt = expiresAt;
    }
    public void Increase(decimal quantity)
    {
        ValidatePositive(quantity);

        _quantity += quantity;
    }
    public void Decrease(decimal quantity)
    {
        ValidatePositive(quantity);

        if (_quantity < quantity)
            throw new InvalidOperationException("Cannot decrease more than reserved quantity.");

        _quantity -= quantity;
    }
    public void Release()
    {
        EnsureActive();
        Status = ReservationStatus.Released;
    }

    public void Fulfill()
    {
        EnsureActive();
        Status = ReservationStatus.Fulfilled;
    }

    public void Cancel()
    {
        EnsureActive();
        Status = ReservationStatus.Cancelled;
    }

    public void Expire()
    {
        if (Status != ReservationStatus.Active)
            return;

        Status = ReservationStatus.Expired;
    }

    public bool IsExpired(DateTimeOffset now)
    {
        if (!ExpiresAt.HasValue)
            return false;

        return now >= ExpiresAt.Value;
    }

    private void EnsureActive()
    {
        if (Status != ReservationStatus.Active)
            throw new ArgumentException("Reservation is not active.");
    }

    private void ValidatePositive(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");
    }

}
