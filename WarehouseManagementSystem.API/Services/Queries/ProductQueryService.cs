using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Services.Queries;

public class ProductQueryService : IProductQueryService
{
    private readonly WarehouseManagementSystemDbContext _context;

    public ProductQueryService(WarehouseManagementSystemDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProductListDto>> GetProductsAsync(CancellationToken ct = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Select(p => new ProductListDto(
                p.Id,
                p.SKU,
                p.Name,
                p.Unit,
                p.RequiresBatch,
                p.Weight,
                p.Volume,
                p.IsActive))
            .ToListAsync(ct);
    }

    public async Task<ProductDetailsDto?> GetProductAsync(Guid productId, CancellationToken ct = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new ProductDetailsDto
            {
                Id = p.Id,
                Sku = p.SKU,
                Name = p.Name,
                Description = p.Description,
                Unit = p.Unit,
                RequiresBatch = p.RequiresBatch,
                IsActive = p.IsActive,
                Weight = p.Weight,
                Volume = p.Volume
            })
            .FirstOrDefaultAsync(ct);
    }
}
