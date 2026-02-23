using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Interfaces.Repositories;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.Infrastructure.Persistence.Repositories;

public class StockReservationRepository : IStockReservationRepository
{
    private readonly WarehouseManagementSystemDbContext _context;

    public StockReservationRepository(WarehouseManagementSystemDbContext context)
    {
        _context = context;
    }

    public void Add(StockReservation entity)
    {
        _context.StockReservations.Add(entity);
    }

    public async Task<IEnumerable<StockReservation>> AllAsync()
    {
        return await _context.StockReservations.ToListAsync();
    }

    public bool Any(Expression<Func<StockReservation, bool>> predicate)
    {
        return _context.StockReservations.Any(predicate);
    }

    public void Delete(StockReservation entity)
    {
        _context.StockReservations.Remove(entity);
    }

    public StockReservation Find(Guid id)
    {
        return _context.StockReservations.Find(id);
    }

    public async Task<StockReservation> FindAsync(Guid id)
    {
        return await _context.StockReservations.FindAsync(id);
    }

    public async Task<IEnumerable<StockReservation>> GetActiveReservationsAsync(Guid stockId)
    {
        return await _context.StockReservations.Where(r => r.StockId == stockId && r.Status == ReservationStatus.Active)
                    .OrderBy(r => r.CreatedAt).ToListAsync();
    }

    public async Task<IReadOnlyCollection<StockReservation>> GetExpiredReservationsAsync(DateTimeOffset currentTime)
    {
        return (await _context.StockReservations.Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt.HasValue && r.ExpiresAt <= currentTime)
                    .OrderBy(r => r.ExpiresAt).AsNoTracking().ToListAsync()).AsReadOnly();
    }
    public async Task<IReadOnlyCollection<StockReservation>> GetActiveReservationsByDocumentIdAsync(Guid documentId)
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
                  && (
                      (stock.WarehouseId == item.Document.SourceWarehouseId)
                      || (stock.WarehouseId == item.Document.TargetWarehouseId)
                  )
            select reservation
        ).ToListAsync();

        return reservations;
    }

    public StockReservation Update(StockReservation entity)
    {
        return _context.StockReservations.Update(entity).Entity;
    }

    public void UpdateRange(IEnumerable<StockReservation> entities)
    {
        _context.StockReservations.UpdateRange(entities);
    }
}

