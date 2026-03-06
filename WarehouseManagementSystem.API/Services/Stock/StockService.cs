using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Services;

namespace WarehouseManagementSystem.API.Services.Stocks;
//TODO poprawić serwis o aktualizację klasy Stock
public class StockService : IStockService
{
    private readonly IUnitOfWork _unitOfWork;

    public StockService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Stock> GetOrCreateAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, Guid? batchId)
    {
        var stock = await _unitOfWork.Stocks.GetByProductAndWarehouseAsync(productId, warehouseId, warehouseZoneId, batchId);
        if (stock != null) return stock;

        stock = new Stock(productId, warehouseId, warehouseZoneId, batchId, 0m);
        _unitOfWork.Stocks.Add(stock);
        await _unitOfWork.SaveChangesAsync();
        return stock;
    }

    public async Task IncreaseStockAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, decimal quantity, Guid? batchId)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        var stock = await GetOrCreateAsync(productId, warehouseId, warehouseZoneId, batchId);
        stock.Increase(quantity);

        _unitOfWork.Stocks.Update(stock);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DecreaseStockAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, decimal quantity, Guid? batchId)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        var stock = await GetOrCreateAsync(productId, warehouseId, warehouseZoneId, batchId);
        stock.Decrease(quantity);

        _unitOfWork.Stocks.Update(stock);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<bool> IsAvailableAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, decimal requiredQuantity, Guid? batchId)
    {
        if (requiredQuantity <= 0) return true;

        var stock = await GetOrCreateAsync(productId, warehouseId, warehouseZoneId, batchId);
        return stock.Available >= requiredQuantity;
    }

    public async Task MoveStockAsync(Guid productId, Guid sourceWarehouseId, Guid sourceZoneId, Guid targetWarehouseId, Guid targetZoneId, decimal quantity, Guid? batchId)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        var sourceStock = await GetOrCreateAsync(productId, sourceWarehouseId, sourceZoneId, batchId);
        var targetStock = await GetOrCreateAsync(productId, targetWarehouseId, targetZoneId, batchId);

        sourceStock.Decrease(quantity);
        targetStock.Increase(quantity);

        _unitOfWork.Stocks.Update(sourceStock);
        _unitOfWork.Stocks.Update(targetStock);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ReserveStockAsync(Guid stockId, decimal quantity, string reservationSource, Guid createdBy, DateTimeOffset? expiresAt = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        var stock = await _unitOfWork.Stocks.FindAsync(stockId)
                    ?? throw new InvalidOperationException("Stock not found.");

        if (stock.Available < quantity)
            throw new InvalidOperationException("Not enough available stock to reserve.");

        stock.Reserve(quantity);
        _unitOfWork.Stocks.Update(stock);

        var reservation = new StockReservation(
            stockId,
            quantity,
            reservationSource,
            createdBy,
            expiresAt
        );

        _unitOfWork.StockReservations.Add(reservation);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ReleaseReservationAsync(Guid stockId, decimal quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        var stock = await _unitOfWork.Stocks.FindAsync(stockId)
                    ?? throw new InvalidOperationException("Stock not found.");

        var reservations = await _unitOfWork.StockReservations.GetActiveReservationsAsync(stock.Id);

        var remaining = quantity;

        foreach (var reservation in reservations.OrderBy(r => r.CreatedAt))
        {
            var toRelease = Math.Min(reservation.Quantity, remaining);
            reservation.Decrease(toRelease);

            if (reservation.Quantity == 0)
                reservation.Release();

            _unitOfWork.StockReservations.Update(reservation);

            remaining -= toRelease;
            if (remaining <= 0) break;
        }
        stock.Unreserve(quantity); 
        _unitOfWork.Stocks.Update(stock);

        await _unitOfWork.SaveChangesAsync();
    }
}