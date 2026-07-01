using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.Caching;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Services.AuditLogs.Query;

public class AuditLogQueryService : IAuditLogQueryService
{
    private const string ContractVersion = "v1";

    private readonly WarehouseManagementSystemDbContext _context;
    private readonly IQueryCacheService _queryCache;

    public AuditLogQueryService(WarehouseManagementSystemDbContext context, IQueryCacheService queryCache)
    {
        _context = context;
        _queryCache = queryCache;
    }

    public AuditLogQueryService(WarehouseManagementSystemDbContext context)
        : this(context, new NoOpQueryCacheService())
    {
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetFilteredAsync(
        string? entityName,
        Guid? entityId,
        Guid? performedById,
        CancellationToken ct = default)
    {
        // Są to parametry, które będą używane do filtrowania wyników zapytania do bazy danych. W tym przypadku są to:
        var parameters = new Dictionary<string, string>
        {
            ["entityName"] = CacheKeyNormalizer.NormalizeString(entityName),
            ["entityId"] = CacheKeyNormalizer.NormalizeGuid(entityId),
            ["performedById"] = CacheKeyNormalizer.NormalizeGuid(performedById)
        };

        return await _queryCache.GetOrCreateAsync(
                   CacheRegions.AuditLogs,
                   ContractVersion,
                   parameters,
                   async token =>
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
                           .ToListAsync(token);
                   },
                   ct)
               ?? new List<AuditLogDto>();
    }

    public async Task<AuditLogDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["id"] = id.ToString("D")
        };

        return await _queryCache.GetOrCreateAsync(
            CacheRegions.AuditLogs,
            ContractVersion,
            parameters,
            async token => await _context.AuditLogs
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
                .FirstOrDefaultAsync(token),
            ct);
    }

    private sealed class NoOpQueryCacheService : IQueryCacheService // jest użyty jako domyślna implementacja, gdy nie chcemy korzystać z cache'owania w testach lub w prostych scenariuszach. 
    {
        public Task<T?> GetOrCreateAsync<T>(
            string region,
            string contractVersion,
            IReadOnlyDictionary<string, string> parameters,
            Func<CancellationToken, Task<T?>> factory,
            CancellationToken ct = default)
        {
            return factory(ct);
        }
    }
}
