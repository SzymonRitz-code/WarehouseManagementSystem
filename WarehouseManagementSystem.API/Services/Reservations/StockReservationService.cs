using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Services;
using WarehouseManagementSystem.Infrastructure.Services;

namespace WarehouseManagementSystem.API.Services.Reservations;

public class StockReservationService : IStockReservationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemClock clock;

    public StockReservationService(IUnitOfWork unitOfWork, ISystemClock clock)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        this.clock = clock;
    }

    public async Task<StockReservation> CreateReservationAsync(Guid stockId, decimal quantity, string source, Guid createdBy, DateTimeOffset? expiresAt = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        var stock = await _unitOfWork.Stocks.FindAsync(stockId)
                    ?? throw new InvalidOperationException("Stock not found.");

        if (stock.Available < quantity)
            throw new InvalidOperationException("Not enough available stock to reserve.");

        stock.Reserve(quantity);
        _unitOfWork.Stocks.Update(stock);

        var reservation = new StockReservation(stockId, quantity, source, createdBy, expiresAt);
        _unitOfWork.StockReservations.Add(reservation);

        await _unitOfWork.SaveChangesAsync();

        return reservation;
    }

    public async Task ReleaseReservationAsync(Guid reservationId)
    {
        var reservation = await _unitOfWork.StockReservations.FindAsync(reservationId)
                          ?? throw new InvalidOperationException("Reservation not found.");

        var stock = await _unitOfWork.Stocks.FindAsync(reservation.StockId)
                    ?? throw new InvalidOperationException("Stock not found.");

        stock.Unreserve(reservation.Quantity);
        reservation.Release();

        _unitOfWork.Stocks.Update(stock);
        _unitOfWork.StockReservations.Update(reservation);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task CancelReservationAsync(Guid reservationId)
    {
        var reservation = await _unitOfWork.StockReservations.FindAsync(reservationId)
                          ?? throw new InvalidOperationException("Reservation not found.");

        var stock = await _unitOfWork.Stocks.FindAsync(reservation.StockId)
                    ?? throw new InvalidOperationException("Stock not found.");

        stock.Unreserve(reservation.Quantity);

        _unitOfWork.Stocks.Update(stock);
        _unitOfWork.StockReservations.Delete(reservation);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ConfirmReservationAsync(Guid reservationId)
    {
        var reservation = await _unitOfWork.StockReservations.FindAsync(reservationId)
                          ?? throw new InvalidOperationException("Reservation not found.");

        var stock = await _unitOfWork.Stocks.FindAsync(reservation.StockId)
                    ?? throw new InvalidOperationException("Stock not found.");

        stock.Decrease(reservation.Quantity);
        reservation.Release();

        _unitOfWork.Stocks.Update(stock);
        _unitOfWork.StockReservations.Update(reservation);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<StockReservation>> GetActiveReservationsAsync(Guid stockId)
    {
        var reservations = await _unitOfWork.StockReservations.GetActiveReservationsAsync(stockId);
        return reservations.ToList().AsReadOnly();
    }

    public async Task ExpireReservationsAsync()
    {
        var now = clock.UtcNow;
        var expiredReservations = await _unitOfWork.StockReservations.GetExpiredReservationsAsync(now);

        foreach (var reservation in expiredReservations)
        {
            var stock = await _unitOfWork.Stocks.FindAsync(reservation.StockId);
            if (stock != null)
            {
                stock.Unreserve(reservation.Quantity);
                _unitOfWork.Stocks.Update(stock);
            }

            reservation.Release();
            _unitOfWork.StockReservations.Update(reservation);
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
