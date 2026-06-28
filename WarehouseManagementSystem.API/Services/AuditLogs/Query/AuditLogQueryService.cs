using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Services.AuditLogs.Query;

public class AuditLogQueryService : IAuditLogQueryService
{
    private readonly WarehouseManagementSystemDbContext _context;

    public AuditLogQueryService(WarehouseManagementSystemDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetFilteredAsync(
        string? entityName,
        Guid? entityId,
        Guid? performedById,
        CancellationToken ct = default)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(x => x.EntityName == entityName);
        }

        if (entityId.HasValue)
        {
            query = query.Where(x => x.EntityId == entityId.Value);
        }

        if (performedById.HasValue)
        {
            query = query.Where(x => x.PerformedById == performedById.Value);
        }

        return await query
            .OrderByDescending(x => x.PerformedAt)
            .Select(x => new AuditLogDto(
                x.Id,
                x.EntityName,
                x.EntityId,
                x.Operation,
                x.OldValues,
                x.NewValues,
                x.PerformedAt,
                x.IpAddress,
                x.PerformedById,
                string.Empty,
                string.Empty))
            .ToListAsync(ct);
    }

    public async Task<AuditLogDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AuditLogDto(
                x.Id,
                x.EntityName,
                x.EntityId,
                x.Operation,
                x.OldValues,
                x.NewValues,
                x.PerformedAt,
                x.IpAddress,
                x.PerformedById,
                string.Empty,
                string.Empty))
            .FirstOrDefaultAsync(ct);
    }
}
