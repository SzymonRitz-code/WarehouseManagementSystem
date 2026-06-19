using WarehouseManagementSystem.API.DTO;

namespace WarehouseManagementSystem.API.Services.Queries;

public interface IProductQueryService
{
    Task<IReadOnlyList<ProductListDto>> GetProductsAsync(CancellationToken ct = default);
    Task<PagedResult<ProductListDto>> GetProductsPageAsync(ProductListQuery query, CancellationToken ct = default);
    Task<ProductDetailsDto?> GetProductAsync(Guid productId, CancellationToken ct = default);
}
