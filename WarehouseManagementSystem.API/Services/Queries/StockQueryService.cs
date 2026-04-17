using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Services.Queries;

public class StockQueryService : IStockQueryService
{
    private readonly WarehouseManagementSystemDbContext _context;

    public StockQueryService(WarehouseManagementSystemDbContext context)
    {
        _context = context;
    }
    public async Task<List<StockDto>> GetStocksAsync(CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Select(s => new StockDto{
                Id = s.Id,
                ProductId = s.ProductId,
                ProductSku = s.Product.SKU,
                ProductName = s.Product.Name,
                WarehouseId = s.WarehouseId,
                WarehouseName = s.Warehouse.Name,
                ZoneName = s.WarehouseZone.Name,
                QuantityAvailable = s.QuantityTotal - s.QuantityReserved,
                QuantityReserved = s.QuantityReserved,
                QuantityTotal = s.QuantityTotal,
                Unit = s.Product.Unit.ToString(),
                LastUpdated = s.LastUpdated
            })
            .ToListAsync();    
    }
    //public async Task<List<StockMoveDto>> GetStockMoves()
    //{
    //    return await _context.StockMoves
    //        .AsNoTracking()
    //        .Include(x => x.Product)
    //        .Include(x => x.FromWarehouse)
    //        .Include(x => x.FromZone)
    //        .Include(x => x.ToWarehouse)
    //        .Include(x => x.ToZone)
    //        .OrderByDescending(x => x.MovedAt)
    //        .Select(x => new StockMoveDto(
    //            x.Id,
    //            x.Product.SKU,
    //            x.Product.Name,
    //            x.FromWarehouse.Name,
    //            x.FromZone.Name,
    //            x.ToWarehouse.Name,
    //            x.ToZone.Name,
    //            x.Quantity,
    //            x.Product.Unit.ToString(),
    //            x.MoveType.ToString(),
    //            x.Status.ToString(),
    //            x.MovedBy.ToString(),
    //            x.MovedAt,
    //            x.Reference
    //        ))
    //        .ToListAsync();
    //}
    public async Task<Stock?> GetByIdAsync(Guid stockId, CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stockId, ct);
    }

    public async Task<Stock?> GetStockAsync(
        Guid productId,
        Guid? batchId,
        Guid warehouseId,
        Guid? warehouseZoneId,
        CancellationToken ct = default)
    {
        var query = _context.Stocks
            .AsNoTracking()
            .Where(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId);

        if (batchId.HasValue)
            query = query.Where(s => s.ProductBatchId == batchId.Value);

        if (warehouseZoneId.HasValue)
            query = query.Where(s => s.WarehouseZoneId == warehouseZoneId.Value);

        return await query.FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Stock>> GetByProductAsync(
        Guid productId,
        CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s => s.ProductId == productId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Stock>> GetByWarehouseAsync(
        Guid warehouseId,
        CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s => s.WarehouseId == warehouseId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Stock>> GetByWarehouseZoneAsync(
        Guid warehouseZoneId,
        CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s => s.WarehouseZoneId == warehouseZoneId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Stock>> GetByProductAndWarehouseAsync(
        Guid productId,
        Guid warehouseId,
        CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId)
            .ToListAsync(ct);
    }

    public async Task<decimal> GetAvailableQuantityAsync(
        Guid productId,
        Guid? batchId,
        Guid warehouseId,
        Guid? warehouseZoneId,
        CancellationToken ct = default)
    {
        var query = _context.Stocks
            .AsNoTracking()
            .Where(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId);

        if (batchId.HasValue)
            query = query.Where(s => s.ProductBatchId == batchId.Value);

        if (warehouseZoneId.HasValue)
            query = query.Where(s => s.WarehouseZoneId == warehouseZoneId.Value);

        return await query
            .Select(s => s.QuantityTotal - s.QuantityReserved)
            .DefaultIfEmpty(0m)
            .SumAsync(ct);
    }

    public async Task<decimal> GetTotalQuantityAsync(
        Guid productId,
        Guid warehouseId,
        CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId)
            .Select(s => s.QuantityTotal)
            .DefaultIfEmpty(0m)
            .SumAsync(ct);
    }

    public async Task<bool> IsAvailableAsync(
        Guid productId,
        Guid warehouseId,
        Guid warehouseZoneId,
        decimal requiredQuantity,
        Guid? batchId,
        CancellationToken ct = default)
    {
        if (requiredQuantity <= 0)
            throw new ArgumentException("Required quantity must be greater than zero.", nameof(requiredQuantity));

        var available = await _context.Stocks
            .AsNoTracking()
            .Where(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId &&
                s.WarehouseZoneId == warehouseZoneId &&
                s.ProductBatchId == batchId)
            .Select(s => s.QuantityTotal - s.QuantityReserved)
            .FirstOrDefaultAsync(ct);

        return available >= requiredQuantity;
    }

    public async Task<IReadOnlyList<Stock>> GetStocksWithActiveReservationsAsync(
        CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s => s.Reservations.Any(r => r.Status == ReservationStatus.Active))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Stock>> GetByTemperatureAsync(
        TemperatureType temperatureType,
        CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Include(s => s.WarehouseZone)
            .Where(s => s.WarehouseZone.TemperatureType == temperatureType)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Stock>> GetAvailableForPickingAsync(
        Guid warehouseId,
        CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s =>
                s.WarehouseId == warehouseId &&
                (s.QuantityTotal - s.QuantityReserved) > 0)
            .OrderByDescending(s => s.QuantityTotal - s.QuantityReserved)
            .ToListAsync(ct);
    }
}