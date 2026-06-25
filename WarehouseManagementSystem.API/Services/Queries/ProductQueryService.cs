using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.API.Services.Queries;

public class ProductQueryService : IProductQueryService
{
    #region Fields and Constructor

    private readonly WarehouseManagementSystemDbContext _context;

    public ProductQueryService(WarehouseManagementSystemDbContext context)
    {
        _context = context;
    }

    #endregion

    #region Product Query Operations

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

    public async Task<PagedResult<ProductListDto>> GetProductsPageAsync(ProductListQuery query, CancellationToken ct = default)
    {
        var products = BuildProductListQuery();

        products = ApplyProductListSearch(products, query);

        var totalItems = await products.CountAsync(ct);
        var orderedProducts = ApplyProductListSorting(products, query.SortBy, query.SortDirection);

        var items = await orderedProducts
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
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

        return new PagedResult<ProductListDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalItems = totalItems
        };
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

    #endregion

    #region Query Helpers

    private IQueryable<Product> BuildProductListQuery()
    {
        return _context.Products
            .AsNoTracking();
    }

    private static IQueryable<Product> ApplyProductListSearch(
        IQueryable<Product> products,
        ProductListQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            products = products.Where(p =>
                p.SKU.Contains(search) ||
                p.Name.Contains(search));
        }

        if (query.Unit.HasValue)
        {
            products = products.Where(p => p.Unit == query.Unit.Value);
        }

        if (query.RequiresBatch.HasValue)
        {
            products = products.Where(p => p.RequiresBatch == query.RequiresBatch.Value);
        }

        if (query.IsActive.HasValue)
        {
            products = products.Where(p => p.IsActive == query.IsActive.Value);
        }

        return products;
    }

    private static IQueryable<Product> ApplyProductListSorting(
        IQueryable<Product> products,
        string? sortBy,
        string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var sortKey = sortBy?.Trim().ToLowerInvariant();

        return sortKey switch
        {
            "name" => descending
                ? products.OrderByDescending(p => p.Name).ThenBy(p => p.SKU)
                : products.OrderBy(p => p.Name).ThenBy(p => p.SKU),
            "unit" => descending
                ? products.OrderByDescending(p => p.Unit).ThenBy(p => p.SKU)
                : products.OrderBy(p => p.Unit).ThenBy(p => p.SKU),
            "requiresbatch" => descending
                ? products.OrderByDescending(p => p.RequiresBatch).ThenBy(p => p.SKU)
                : products.OrderBy(p => p.RequiresBatch).ThenBy(p => p.SKU),
            "weight" => descending
                ? products.OrderByDescending(p => p.Weight).ThenBy(p => p.SKU)
                : products.OrderBy(p => p.Weight).ThenBy(p => p.SKU),
            "volume" => descending
                ? products.OrderByDescending(p => p.Volume).ThenBy(p => p.SKU)
                : products.OrderBy(p => p.Volume).ThenBy(p => p.SKU),
            "isactive" => descending
                ? products.OrderByDescending(p => p.IsActive).ThenBy(p => p.SKU)
                : products.OrderBy(p => p.IsActive).ThenBy(p => p.SKU),
            _ => descending
                ? products.OrderByDescending(p => p.SKU)
                : products.OrderBy(p => p.SKU)
        };
    }

    #endregion
}
