using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Services.Queries;

public class DocumentQueryService : IDocumentQueryService
{
    private readonly WarehouseManagementSystemDbContext _context;

    public DocumentQueryService(WarehouseManagementSystemDbContext context)
    {
        _context = context;
    }

    public async Task<Document?> GetByIdAsync(Guid documentId, CancellationToken ct = default)
    {
        return await _context.Documents
            .Include(d => d.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);
    }

    public async Task<Document?> GetByNumberAsync(string number, CancellationToken ct = default)
    {
        return await _context.Documents
            .Include(d => d.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Number == number, ct);
    }

    public async Task<IReadOnlyList<Document>> GetByStatusAsync(
        DocumentStatus status,
        CancellationToken ct = default)
    {
        return await _context.Documents
            .Where(d => d.Status == status)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetByTypeAsync(
        DocumentType type,
        CancellationToken ct = default)
    {
        return await _context.Documents
            .Where(d => d.Type == type)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetByWarehouseAsync(
        Guid warehouseId,
        CancellationToken ct = default)
    {
        return await _context.Documents
            .Where(d =>
                d.SourceWarehouseId == warehouseId ||
                d.TargetWarehouseId == warehouseId)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetBetweenDatesAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        return await _context.Documents
            .Where(d => d.CreatedAt >= from && d.CreatedAt <= to)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        return await _context.Documents
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid documentId, CancellationToken ct = default)
    {
        return await _context.Documents.AsNoTracking()
            .AnyAsync(d => d.Id == documentId, ct);
    }

    public async Task<IReadOnlyList<Document>> GetByTypeAndStatusAsync(
        DocumentType type,
        DocumentStatus status,
        CancellationToken ct = default)
    {
        return await _context.Documents
            .Where(d => d.Type == type && d.Status == status)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetDraftsAsync(CancellationToken ct = default)
    {
        return await _context.Documents
            .Where(d => d.Status == DocumentStatus.Draft)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetPendingConfirmationAsync(CancellationToken ct = default)
    {
        // Dokumenty w trakcie procesu operacyjnego
        return await _context.Documents
            .Where(d => d.Status == DocumentStatus.Draft)
            .OrderBy(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StockReservation>> GetActiveReservationsAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        return await (
            from item in _context.DocumentItems
            join stock in _context.Stocks
                on new { item.ProductId, item.ProductBatchId }
                equals new { stock.ProductId, stock.ProductBatchId }
            join reservation in _context.StockReservations
                on stock.Id equals reservation.StockId
            join document in _context.Documents
                on item.DocumentId equals document.Id
            where item.DocumentId == documentId
                  && reservation.Status == ReservationStatus.Active
                  && (
                        (document.SourceWarehouseId.HasValue &&
                         stock.WarehouseId == document.SourceWarehouseId &&
                         stock.WarehouseZoneId == item.SourceZoneId)
                     ||
                        (document.TargetWarehouseId.HasValue &&
                         stock.WarehouseId == document.TargetWarehouseId &&
                         stock.WarehouseZoneId == item.TargetZoneId)
                     )
            select reservation
        ).AsNoTracking().ToListAsync(ct);
    }

    public async Task<bool> HasActiveReservationsAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        return await (
            from item in _context.DocumentItems.AsNoTracking()
            join stock in _context.Stocks.AsNoTracking()
                on new { item.ProductId, item.ProductBatchId }
                equals new { stock.ProductId, stock.ProductBatchId }
            join reservation in _context.StockReservations.AsNoTracking()
                on stock.Id equals reservation.StockId
            join document in _context.Documents.AsNoTracking()
                on item.DocumentId equals document.Id
            where item.DocumentId == documentId
                  && reservation.Status == ReservationStatus.Active
                  && (
                        (document.SourceWarehouseId.HasValue &&
                         stock.WarehouseId == document.SourceWarehouseId &&
                         stock.WarehouseZoneId == item.SourceZoneId)
                     ||
                        (document.TargetWarehouseId.HasValue &&
                         stock.WarehouseId == document.TargetWarehouseId &&
                         stock.WarehouseZoneId == item.TargetZoneId)
                     )
            select reservation.Id
        ).AnyAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetRecentAsync(
        int take,
        CancellationToken ct = default)
    {
        return await _context.Documents
            .OrderByDescending(d => d.CreatedAt)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
