using WarehouseManagementSystem.API.DTO;

namespace WarehouseManagementSystem.API.Services.ProductBatches.Query;

/// <summary>
/// Defines product batch read operations.
/// </summary>
public interface IProductBatchQueryService
{
    /// <summary>
    /// Gets the list of all product batches.
    /// </summary>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Product batch list.</returns>
    Task<IReadOnlyList<ProductBatchListDto>> GetBatchesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets product batches assigned to a specific product.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Product batch list assigned to the product.</returns>
    Task<IReadOnlyList<ProductBatchListDto>> GetBatchesByProductAsync(Guid productId, CancellationToken ct = default);

    /// <summary>
    /// Gets product batch list item by identifier.
    /// </summary>
    /// <param name="batchId">Product batch identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Product batch list item, or <c>null</c> if not found.</returns>
    Task<ProductBatchListDto?> GetBatchListItemAsync(Guid batchId, CancellationToken ct = default);

    /// <summary>
    /// Gets product batch details by identifier.
    /// </summary>
    /// <param name="batchId">Product batch identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Product batch details, or <c>null</c> if not found.</returns>
    Task<ProductBatchDto?> GetBatchAsync(Guid batchId, CancellationToken ct = default);

    /// <summary>
    /// Gets product batch details by product and batch identifiers.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="batchId">Product batch identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Product batch details, or <c>null</c> if not found in product scope.</returns>
    Task<ProductBatchDto?> GetBatchForProductAsync(Guid productId, Guid batchId, CancellationToken ct = default);
}
