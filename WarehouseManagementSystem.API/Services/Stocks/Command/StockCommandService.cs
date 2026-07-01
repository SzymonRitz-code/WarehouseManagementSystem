using WarehouseManagementSystem.API.Caching;
using WarehouseManagementSystem.Domain.Exceptions;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.ValueObjects;
using WarehouseManagementSystem.Infrastructure.Services;

namespace WarehouseManagementSystem.API.Services.Stocks.Command;

public class StockCommandService : IStockCommandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemClock _clock;
    private readonly ICacheInvalidationService _cacheInvalidation;

    public StockCommandService(IUnitOfWork unitOfWork, ISystemClock clock, ICacheInvalidationService cacheInvalidation)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _cacheInvalidation = cacheInvalidation ?? throw new ArgumentNullException(nameof(cacheInvalidation));
    }

    #region Stock Creation / Retrieval

    public virtual async Task<Stock> GetOrCreateAsync(
        Guid productId,
        Guid warehouseId,
        Guid warehouseZoneId,
        Guid? batchId,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var stock = await _unitOfWork.Stocks
            .GetByProductAndWarehouseAsync(productId, warehouseId, warehouseZoneId, batchId);

        if (stock != null)
        {
            return stock;
        }

        stock = new Stock(productId, warehouseId, warehouseZoneId, batchId, 0m);

        _unitOfWork.Stocks.Add(stock);

        return stock;
    }

    #endregion

    #region Stock Quantity Operations

    public async Task IncreaseStockAsync(
        Guid productId,
        Guid warehouseId,
        Guid warehouseZoneId,
        decimal quantity,
        Guid? batchId,
        CancellationToken ct = default)
    {
        EnsurePositiveQuantity(quantity);

        var stock = await GetOrCreateAsync(productId, warehouseId, warehouseZoneId, batchId, ct);
        stock.Increase(quantity);

        await _unitOfWork.SaveChangesAsync(ct);
        await InvalidateAsync(ct);
    }

    public async Task DecreaseStockAsync(
        Guid productId,
        Guid warehouseId,
        Guid warehouseZoneId,
        decimal quantity,
        Guid? batchId,
        CancellationToken ct = default)
    {
        EnsurePositiveQuantity(quantity);

        var stock = await GetOrCreateAsync(productId, warehouseId, warehouseZoneId, batchId, ct);
        stock.Decrease(quantity);

        await _unitOfWork.SaveChangesAsync(ct);
        await InvalidateAsync(ct);
    }

    public async Task MoveStockAsync(
        Guid productId,
        Guid sourceWarehouseId,
        Guid sourceZoneId,
        Guid targetWarehouseId,
        Guid targetZoneId,
        decimal quantity,
        Guid? batchId,
        CancellationToken ct = default)
    {
        EnsurePositiveQuantity(quantity);

        var sourceStock = await GetOrCreateAsync(productId, sourceWarehouseId, sourceZoneId, batchId, ct);
        var targetStock = await GetOrCreateAsync(productId, targetWarehouseId, targetZoneId, batchId, ct);

        sourceStock.Decrease(quantity);
        targetStock.Increase(quantity);

        await _unitOfWork.SaveChangesAsync(ct);
        await InvalidateAsync(ct);
    }

    #endregion

    #region Reservation Operations

    public async Task<StockReservation> ReserveStockAsync(
        Guid stockId,
        decimal quantity,
        string reservationSource,
        UserSnapshot createdBy,
        DateTimeOffset? expiresAt = null,
        CancellationToken ct = default)
    {
        EnsurePositiveQuantity(quantity);

        var stock = await GetStockOrThrowAsync(stockId, ct);

        var reservation = stock.CreateReservation(quantity, reservationSource, createdBy, expiresAt);

        await _unitOfWork.SaveChangesAsync(ct);
        await InvalidateAsync(ct);
        return reservation;
    }

    public async Task ReleaseReservationAsync(Guid stockId, Guid reservationId, CancellationToken ct = default)
    {
        var stock = await GetStockOrThrowAsync(stockId, ct);
        EnsureReservationExists(stock, reservationId);

        stock.ReleaseReservation(reservationId);

        await _unitOfWork.SaveChangesAsync(ct);
        await InvalidateAsync(ct);
    }

    public async Task CancelReservationAsync(Guid reservationId, CancellationToken ct = default)
    {
        var stock = await GetStockContainingReservationOrThrowAsync(reservationId, ct);

        stock.CancelReservation(reservationId);

        await _unitOfWork.SaveChangesAsync(ct);
        await InvalidateAsync(ct);
    }

    public async Task ConfirmReservationAsync(Guid reservationId, CancellationToken ct = default)
    {
        var stock = await GetStockContainingReservationOrThrowAsync(reservationId, ct);

        stock.ConfirmReservation(reservationId);

        await _unitOfWork.SaveChangesAsync(ct);
        await InvalidateAsync(ct);
    }

    public async Task ExpireReservationsAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var expiredReservations = await _unitOfWork.Stocks.GetExpiredReservationsAsync(now);

        foreach (var reservation in expiredReservations)
        {
            ct.ThrowIfCancellationRequested();

            var stock = await _unitOfWork.Stocks.FindAsync(reservation.StockId);
            if (stock == null)
            {
                continue;
            }

            stock.ExpireReservation(reservation.Id);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        await InvalidateAsync(ct);
    }

    #endregion

    private async Task<Stock> GetStockOrThrowAsync(Guid stockId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await _unitOfWork.Stocks.FindAsync(stockId)
               ?? throw new StockNotFoundException(stockId);
    }

    private async Task<Stock> GetStockContainingReservationOrThrowAsync(Guid reservationId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var stock = (await _unitOfWork.Stocks.All())
            .FirstOrDefault(s => s.Reservations.Any(r => r.Id == reservationId));

        return stock ?? throw new ReservationNotFoundException(reservationId);
    }

    private static void EnsureReservationExists(Stock stock, Guid reservationId)
    {
        if (!stock.Reservations.Any(r => r.Id == reservationId))
        {
            throw new ReservationNotFoundException(reservationId);
        }
    }

    private static void EnsurePositiveQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }
    }

    private async Task InvalidateAsync(CancellationToken ct)
    {
        if (_unitOfWork.HasActiveTransaction)
        {
            return;
        }

        await _cacheInvalidation.InvalidateRegionsAsync(new[]
        {
            CacheRegions.Stocks,
            CacheRegions.Warehouses,
            CacheRegions.WarehouseZones,
            CacheRegions.ProductBatches
        }, ct);
    }
}
