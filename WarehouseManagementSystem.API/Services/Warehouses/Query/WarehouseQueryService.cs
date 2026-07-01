using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.Caching;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Services.Warehouses.Query;

public class WarehouseQueryService : IWarehouseQueryService
{
    private const string ContractVersion = "v1";

    private readonly WarehouseManagementSystemDbContext _context;
    private readonly IQueryCacheService _queryCache;

    public WarehouseQueryService(WarehouseManagementSystemDbContext context, IQueryCacheService queryCache)
    {
        _context = context;
        _queryCache = queryCache;
    }

    public WarehouseQueryService(WarehouseManagementSystemDbContext context)
        : this(context, new NoOpQueryCacheService())
    {
    }

    public async Task<IReadOnlyList<WarehouseListDto>> GetWarehousesAsync(CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["scope"] = "all"
        };

        return await _queryCache.GetOrCreateAsync(
                   CacheRegions.Warehouses,
                   ContractVersion,
                   parameters,
                   async token => await _context.Warehouses
                       .AsNoTracking()
                       .Select(w => new WarehouseListDto(
                           w.Id,
                           w.Code,
                           w.Name,
                           w.Country,
                           w.Address,
                           _context.WarehouseZones.AsNoTracking().Count(z => z.WarehouseId == w.Id),
                           _context.Stocks.AsNoTracking().Count(s => s.WarehouseId == w.Id),
                           _context.Stocks
                               .AsNoTracking()
                               .Where(s => s.WarehouseId == w.Id)
                               .Select(s => (decimal?)s.QuantityTotal)
                               .Sum() ?? 0m,
                           w.IsActive,
                           w.CreatedAt))
                       .ToListAsync(token),
                   ct)
               ?? new List<WarehouseListDto>();
    }

    public async Task<WarehouseDetailsDto?> GetWarehouseAsync(Guid warehouseId, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["warehouseId"] = warehouseId.ToString("D")
        };

        return await _queryCache.GetOrCreateAsync(
            CacheRegions.Warehouses,
            ContractVersion,
            parameters,
            async token => await _context.Warehouses
                .AsNoTracking()
                .Where(w => w.Id == warehouseId)
                .Select(w => new WarehouseDetailsDto
                {
                    Id = w.Id,
                    Code = w.Code,
                    Name = w.Name,
                    Country = w.Country,
                    City = w.City,
                    Address = w.Address,
                    IsActive = w.IsActive
                })
                .FirstOrDefaultAsync(token),
            ct);
    }

    public async Task<IReadOnlyList<WarehouseZoneListDto>> GetWarehouseZonesAsync(CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["scope"] = "all-zones"
        };

        return await _queryCache.GetOrCreateAsync(
                   CacheRegions.WarehouseZones,
                   ContractVersion,
                   parameters,
                   async token => await _context.WarehouseZones
                       .AsNoTracking()
                       .Select(z => new WarehouseZoneListDto(
                           z.Id,
                           z.Code,
                           z.Name,
                           z.TemperatureType,
                           z.IsPickingZone,
                           z.WarehouseId,
                           z.Warehouse.Name,
                           _context.Stocks
                               .AsNoTracking()
                               .Where(s => s.WarehouseZoneId == z.Id)
                               .Select(s => (decimal?)s.QuantityTotal)
                               .Sum() ?? 0m,
                           z.CreatedAt))
                       .ToListAsync(token),
                   ct)
               ?? new List<WarehouseZoneListDto>();
    }

    public async Task<WarehouseZoneDetailsDto?> GetWarehouseZoneAsync(Guid warehouseZoneId, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["warehouseZoneId"] = warehouseZoneId.ToString("D")
        };

        return await _queryCache.GetOrCreateAsync(
            CacheRegions.WarehouseZones,
            ContractVersion,
            parameters,
            async token => await _context.WarehouseZones
                .AsNoTracking()
                .Where(z => z.Id == warehouseZoneId)
                .Select(z => new WarehouseZoneDetailsDto
                {
                    Id = z.Id,
                    Code = z.Code,
                    Name = z.Name,
                    TemperatureType = z.TemperatureType,
                    IsPickingZone = z.IsPickingZone,
                    WarehouseId = z.WarehouseId,
                    WarehouseName = z.Warehouse.Name
                })
                .FirstOrDefaultAsync(token),
            ct);
    }
    private sealed class NoOpQueryCacheService : IQueryCacheService
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
