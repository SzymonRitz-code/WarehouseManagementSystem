using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Services.ProductBatches.Query;

public class ProductBatchQueryService : IProductBatchQueryService
{
    private readonly WarehouseManagementSystemDbContext _context;

    public ProductBatchQueryService(WarehouseManagementSystemDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProductBatchListDto>> GetBatchesAsync(CancellationToken ct = default)
    {
        return await BuildBatchListQuery()
            .OrderBy(b => b.BatchNumber)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ProductBatchListDto>> GetBatchesByProductAsync(Guid productId, CancellationToken ct = default)
    {
        return await BuildBatchListQuery(productId)
            .OrderBy(b => b.BatchNumber)
            .ToListAsync(ct);
    }

    public async Task<ProductBatchListDto?> GetBatchListItemAsync(Guid batchId, CancellationToken ct = default)
    {
        return await BuildBatchListQuery()
            .FirstOrDefaultAsync(b => b.Id == batchId, ct);
    }

    public async Task<ProductBatchDto?> GetBatchAsync(Guid batchId, CancellationToken ct = default)
    {
        return await _context.ProductBatches
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
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ProductBatchDto?> GetBatchForProductAsync(Guid productId, Guid batchId, CancellationToken ct = default)
    {
        return await _context.ProductBatches
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
            .FirstOrDefaultAsync(ct);
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
}
