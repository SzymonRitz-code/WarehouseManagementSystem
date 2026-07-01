using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.Caching;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Services.ProductBatches.Query;

public class ProductBatchQueryService : IProductBatchQueryService
{
    private const string ContractVersion = "v1";

    private readonly WarehouseManagementSystemDbContext _context;
    private readonly IQueryCacheService _queryCache;

    public ProductBatchQueryService(WarehouseManagementSystemDbContext context, IQueryCacheService queryCache)
    {
        _context = context;
        _queryCache = queryCache;
    }

    public ProductBatchQueryService(WarehouseManagementSystemDbContext context)
        : this(context, new NoOpQueryCacheService())
    {
    }

    public async Task<IReadOnlyList<ProductBatchListDto>> GetBatchesAsync(CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["scope"] = "all"
        };

        return await _queryCache.GetOrCreateAsync(
                   CacheRegions.ProductBatches,
                   ContractVersion,
                   parameters,
                   async token => await BuildBatchListQuery()
                       .OrderBy(b => b.BatchNumber)
                       .ToListAsync(token),
                   ct)
               ?? new List<ProductBatchListDto>();
    }

    public async Task<IReadOnlyList<ProductBatchListDto>> GetBatchesByProductAsync(Guid productId, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["productId"] = productId.ToString("D")
        };

        return await _queryCache.GetOrCreateAsync(
                   CacheRegions.ProductBatches,
                   ContractVersion,
                   parameters,
                   async token => await BuildBatchListQuery(productId)
                       .OrderBy(b => b.BatchNumber)
                       .ToListAsync(token),
                   ct)
               ?? new List<ProductBatchListDto>();
    }

    public async Task<ProductBatchListDto?> GetBatchListItemAsync(Guid batchId, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["batchId"] = batchId.ToString("D"),
            ["scope"] = "list-item"
        };

        return await _queryCache.GetOrCreateAsync(
            CacheRegions.ProductBatches,
            ContractVersion,
            parameters,
            async token => await BuildBatchListQuery()
                .FirstOrDefaultAsync(b => b.Id == batchId, token),
            ct);
    }

    public async Task<ProductBatchDto?> GetBatchAsync(Guid batchId, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["batchId"] = batchId.ToString("D")
        };

        return await _queryCache.GetOrCreateAsync(
            CacheRegions.ProductBatches,
            ContractVersion,
            parameters,
            async token => await _context.ProductBatches
                .AsNoTracking()
                .Where(pb => pb.Id == batchId)
                .Select(pb => new ProductBatchDto
                {
                    Id = pb.Id,
                    BatchNumber = pb.BatchNumber,
                    ProductId = pb.ProductId,
                    ProductName = pb.Product.Name,
                    ManufacturedDate = pb.ManufacturedDate,
                    ExpirationDate = pb.ExpirationDate
                })
                .FirstOrDefaultAsync(token),
            ct);
    }

    public async Task<ProductBatchDto?> GetBatchForProductAsync(Guid productId, Guid batchId, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["productId"] = productId.ToString("D"),
            ["batchId"] = batchId.ToString("D")
        };

        return await _queryCache.GetOrCreateAsync(
            CacheRegions.ProductBatches,
            ContractVersion,
            parameters,
            async token => await _context.ProductBatches
                .AsNoTracking()
                .Where(pb => pb.ProductId == productId && pb.Id == batchId)
                .Select(pb => new ProductBatchDto
                {
                    Id = pb.Id,
                    BatchNumber = pb.BatchNumber,
                    ProductId = pb.ProductId,
                    ProductName = pb.Product.Name,
                    ManufacturedDate = pb.ManufacturedDate,
                    ExpirationDate = pb.ExpirationDate
                })
                .FirstOrDefaultAsync(token),
            ct);
    }

    private IQueryable<ProductBatchListDto> BuildBatchListQuery(Guid? productId = null)
    {
        var batches = _context.ProductBatches.AsNoTracking();

        if (productId.HasValue)
        {
            batches = batches.Where(pb => pb.ProductId == productId.Value);
        }

        return from pb in batches
               join s in _context.Stocks.AsNoTracking() on pb.Id equals s.ProductBatchId into stockGroup
               from s in stockGroup.DefaultIfEmpty()
               group s by new
               {
                   pb.Id,
                   pb.BatchNumber,
                   ProductName = pb.Product.Name,
                   pb.ManufacturedDate,
                   pb.ExpirationDate,
                   pb.CreatedAt
               }
            into g
               select new ProductBatchListDto(
                   g.Key.Id,
                   g.Key.BatchNumber,
                   g.Key.ProductName,
                   g.Key.ManufacturedDate,
                   g.Key.ExpirationDate,
                   (int)g.Sum(s => s != null ? s.QuantityTotal : 0),
                   (int)g.Sum(s => s != null ? s.QuantityTotal - s.QuantityReserved : 0),
                   (int)g.Sum(s => s != null ? s.QuantityReserved : 0),
                   g.Key.CreatedAt);
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
