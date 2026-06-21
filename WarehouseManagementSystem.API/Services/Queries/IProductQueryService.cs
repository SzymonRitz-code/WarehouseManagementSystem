using WarehouseManagementSystem.API.DTO;

namespace WarehouseManagementSystem.API.Services.Queries;

/// <summary>
/// Defines product read operations.
/// </summary>
public interface IProductQueryService
{
    /// <summary>
    /// Gets products for the list view.
    /// </summary>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Product list.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IReadOnlyList<ProductListDto>> GetProductsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a paginated list of products using the provided filters.
    /// </summary>
    /// <param name="query">Filtering, sorting, and pagination parameters for products.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Paginated product list.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<PagedResult<ProductListDto>> GetProductsPageAsync(ProductListQuery query, CancellationToken ct = default);

    /// <summary>
    /// Gets product details by identifier.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Product details, or <c>null</c> if the product does not exist.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<ProductDetailsDto?> GetProductAsync(Guid productId, CancellationToken ct = default);
}
