using System.Linq.Expressions;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.API.Services.Queries;

/// <summary>
/// Defines product batch read operations.
/// </summary>
public interface IProductBatchQueryService
{
    /// <summary>
    /// Gets product batches, optionally limited by a predicate.
    /// </summary>
    /// <param name="predicate">Optional product batch filter predicate.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Product batch list.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IEnumerable<ProductBatchListDto>> GetProductBatchList(Expression<Func<ProductBatch, bool>>? predicate = null, CancellationToken ct = default);

    /// <summary>
    /// Gets product batch details by identifier.
    /// </summary>
    /// <param name="batchId">Product batch identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Product batch details, or <c>null</c> if the batch does not exist.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<ProductBatchDto?> GetProductBatchDetails(Guid batchId, CancellationToken ct = default);
}
