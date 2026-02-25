using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Infrastructure.Persistence;


namespace WarehouseManagementSystem.API.Services.Queries;

public class StockQueryService : IStockQueryService
{
    private readonly WarehouseManagementSystemDbContext _context;

    public StockQueryService(WarehouseManagementSystemDbContext context)
    {
        _context = context;
    }

    public async Task<Stock?> GetByIdAsync(Guid stockId, CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stockId, ct);
    }

    public async Task<Stock?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, Guid? batchId, CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId &&
                s.WarehouseZoneId == warehouseZoneId &&
                s.ProductBatchId == batchId, ct);
    }

    public async Task<IReadOnlyList<Stock>> GetByProductAsync(Guid productId, CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s => s.ProductId == productId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Stock>> GetByWarehouseAsync(Guid warehouseId, CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s => s.WarehouseId == warehouseId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Stock>> GetByWarehouseAndZoneAsync(Guid warehouseId, Guid warehouseZoneId, CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s => s.WarehouseId == warehouseId && s.WarehouseZoneId == warehouseZoneId)
            .ToListAsync(ct);
    }

    public async Task<decimal> GetAvailableQuantityAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, Guid? batchId, CancellationToken ct = default)
    {
        var stock = await GetByProductAndWarehouseAsync(productId, warehouseId, warehouseZoneId, batchId, ct);
        return stock?.Available ?? 0m;
    }

    public async Task<IReadOnlyList<Stock>> GetStocksWithActiveReservationsAsync(CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s => _context.StockReservations
                .Any(r => r.StockId == s.Id && r.Status == ReservationStatus.Active))
            .ToListAsync(ct);
    }
    public async Task<Stock?> GetStockAsync(
    Guid productId,
    Guid? productBatchId,
    Guid warehouseId,
    Guid? warehouseZoneId,
    CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.ProductId == productId &&
                s.ProductBatchId == productBatchId &&
                s.WarehouseId == warehouseId &&
                s.WarehouseZoneId == warehouseZoneId,
                ct);
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
        Guid? productBatchId,
        Guid warehouseId,
        Guid? warehouseZoneId,
        CancellationToken ct = default)
    {
        var query = _context.Stocks
            .AsNoTracking()
            .Where(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId);

        if (productBatchId.HasValue)
            query = query.Where(s => s.ProductBatchId == productBatchId.Value);

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

    public async Task<IReadOnlyList<Stock>> GetByZoneAsync(
        Guid warehouseZoneId,
        CancellationToken ct = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s => s.WarehouseZoneId == warehouseZoneId)
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
                s.Available > 0)
            .OrderByDescending(s => s.Available)
            .ToListAsync(ct);
    }

}
