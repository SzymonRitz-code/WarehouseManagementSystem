using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Services;
using WarehouseManagementSystem.Infrastructure.Services;

namespace WarehouseManagementSystem.API.Services.Stocks;

public class StockService : IStockService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemClock _clock;

    public StockService(IUnitOfWork unitOfWork, ISystemClock clock)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _clock = clock;
    }

    #region Stock Creation / Retrieval

    public async Task<Stock> GetOrCreateAsync(
        Guid productId,
        Guid warehouseId,
        Guid warehouseZoneId,
        Guid? batchId)
    {
        var stock = await _unitOfWork.Stocks
            .GetByProductAndWarehouseAsync(productId, warehouseId, warehouseZoneId, batchId);

        if (stock != null)
            return stock;

        stock = new Stock(productId, warehouseId, warehouseZoneId, batchId, 0m);

        _unitOfWork.Stocks.Add(stock);
        await _unitOfWork.SaveChangesAsync();

        return stock;
    }

    #endregion

    #region Stock Quantity Operations

    public async Task IncreaseStockAsync(
        Guid productId,
        Guid warehouseId,
        Guid warehouseZoneId,
        decimal quantity,
        Guid? batchId)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        var stock = await GetOrCreateAsync(productId, warehouseId, warehouseZoneId, batchId);
        stock.Increase(quantity);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DecreaseStockAsync(
        Guid productId,
        Guid warehouseId,
        Guid warehouseZoneId,
        decimal quantity,
        Guid? batchId)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        var stock = await GetOrCreateAsync(productId, warehouseId, warehouseZoneId, batchId);
        stock.Decrease(quantity);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task MoveStockAsync(
        Guid productId,
        Guid sourceWarehouseId,
        Guid sourceZoneId,
        Guid targetWarehouseId,
        Guid targetZoneId,
        decimal quantity,
        Guid? batchId)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        var sourceStock = await GetOrCreateAsync(productId, sourceWarehouseId, sourceZoneId, batchId);
        var targetStock = await GetOrCreateAsync(productId, targetWarehouseId, targetZoneId, batchId);

        sourceStock.Decrease(quantity);
        targetStock.Increase(quantity);

        await _unitOfWork.SaveChangesAsync();
    }

    #endregion

    #region Reservation Operations

    public async Task<StockReservation> ReserveStockAsync(
        Guid stockId,
        decimal quantity,
        string reservationSource,
        Guid createdBy,
        DateTimeOffset? expiresAt = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        var stock = await _unitOfWork.Stocks.FindAsync(stockId)
                    ?? throw new InvalidOperationException("Stock not found.");

        // Tworzymy rezerwację przez agregat Stock
        var reservation = stock.CreateReservation(quantity, reservationSource, createdBy, expiresAt);

        await _unitOfWork.SaveChangesAsync();
        return reservation;
    }

    public async Task ReleaseReservationAsync(Guid stockId, Guid reservationId)
    {
        var stock = await _unitOfWork.Stocks.FindAsync(stockId)
                    ?? throw new InvalidOperationException("Stock not found.");

        stock.ReleaseReservation(reservationId);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task CancelReservationAsync(Guid reservationId)
    {
        var stock = (await _unitOfWork.Stocks
            .All()).FirstOrDefault(s => s.Reservations.Any(r => r.Id == reservationId));

        if (stock == null)
            throw new InvalidOperationException("Stock not found for reservation.");

        stock.CancelReservation(reservationId);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ConfirmReservationAsync(Guid reservationId)
    {
        var stock = (await _unitOfWork.Stocks
            .All()).FirstOrDefault(s => s.Reservations.Any(r => r.Id == reservationId));

        if (stock == null)
            throw new InvalidOperationException("Stock not found for reservation.");

        stock.ConfirmReservation(reservationId);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ExpireReservationsAsync()
    {
        var now = _clock.UtcNow;
        var expiredReservations = await _unitOfWork.Stocks
            .GetExpiredReservationsAsync(now);

        foreach (var reservation in expiredReservations)
        {
            var stock = await _unitOfWork.Stocks.FindAsync(reservation.StockId);
            if (stock == null) continue;

            stock.ExpireReservation(reservation.Id);
        }

        await _unitOfWork.SaveChangesAsync();
    }
    #endregion
}