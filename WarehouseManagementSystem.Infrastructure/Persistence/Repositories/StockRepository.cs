using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Interfaces.Repositories;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.Infrastructure.Persistence.Repositories;

public class StockRepository : IStockRepository
{
    private readonly WarehouseManagementSystemDbContext _context;

    public StockRepository(WarehouseManagementSystemDbContext context)
    {
        _context = context;
    }

    // ===========================
    // COMMAND METHODS (StockService)
    // ===========================

    public void Add(Stock entity) => _context.Stocks.Add(entity);

    public Stock Update(Stock entity) => _context.Stocks.Update(entity).Entity;

    public void UpdateRange(IEnumerable<Stock> entities) => _context.Stocks.UpdateRange(entities);

    public void Delete(Stock entity) => _context.Remove(entity);

    public async Task<Stock> FindAsync(Guid id) => await _context.Stocks.FindAsync(id);

    public Stock Find(Guid id) => _context.Stocks.Find(id);

    public async Task<Stock?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, Guid? batchId)
    {
        return await _context.Stocks
            .FirstOrDefaultAsync(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId &&
                s.WarehouseZoneId == warehouseZoneId &&
                s.ProductBatchId == batchId);
    }

    // ===========================
    // QUERY METHODS (StockQueryService)
    // ===========================

    public async Task<IEnumerable<Stock>> AllAsNoTrackingAsync()
    {
        return await _context.Stocks.AsNoTracking().ToListAsync();
    }
    public async Task<IEnumerable<Stock>> All()
    {
        return await _context.Stocks.ToListAsync();
    }

    public bool Any(Expression<Func<Stock, bool>> predicate)
    {
        return _context.Stocks.AsNoTracking().Any(predicate);
    }

    public async Task<Stock?> GetByProductAndWarehouseAsNoTrackingAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, Guid? batchId)
    {
        return await _context.Stocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.ProductId == productId &&
                s.WarehouseId == warehouseId &&
                s.WarehouseZoneId == warehouseZoneId &&
                s.ProductBatchId == batchId);
    }

    // ===========================
    // STOCK RESERVATIONS QUERIES
    // ===========================

    public async Task<IReadOnlyList<StockReservation>> GetActiveReservationsAsync(Guid stockId)
    {
        return await _context.StockReservations
            .Where(r => r.StockId == stockId && r.Status == ReservationStatus.Active)
            .OrderBy(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync()
            .ContinueWith(t => t.Result.AsReadOnly());
    }

    public async Task<IReadOnlyList<StockReservation>> GetExpiredReservationsAsync(DateTimeOffset currentTime)
    {
        return await _context.StockReservations
            .Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt.HasValue && r.ExpiresAt <= currentTime)
            .OrderBy(r => r.ExpiresAt)
            .AsNoTracking()
            .ToListAsync()
            .ContinueWith(t => t.Result.AsReadOnly());
    }

    public async Task<IReadOnlyList<StockReservation>> GetActiveReservationsByDocumentIdAsync(Guid documentId)
    {
        var reservations = await (
            from item in _context.DocumentItems
            join stock in _context.Stocks
                on new { item.ProductId, item.ProductBatchId, WarehouseZoneId = item.SourceZoneId ?? Guid.Empty }
                   equals new { stock.ProductId, stock.ProductBatchId, WarehouseZoneId = stock.WarehouseZoneId }
                into stockJoin
            from stock in stockJoin.DefaultIfEmpty()
            join reservation in _context.StockReservations
                on stock.Id equals reservation.StockId
            where item.DocumentId == documentId
                  && reservation.Status == ReservationStatus.Active
                  && (stock.WarehouseId == item.Document.SourceWarehouseId
                      || stock.WarehouseId == item.Document.TargetWarehouseId)
            select reservation
        ).AsNoTracking().ToListAsync();

        return reservations;
    }

    public async Task<IReadOnlyList<StockReservation>> FindReservationsByStockIdAsync(Guid stockId)
    {
        return await _context.StockReservations
            .AsNoTracking()
            .Where(s => s.StockId == stockId)
            .ToListAsync();
    }
}