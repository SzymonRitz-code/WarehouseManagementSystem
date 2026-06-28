using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Services.ProductBatches.Command;

/// <summary>
/// Defines product batch operations that change state.
/// </summary>
public interface IProductBatchCommandService
{
    /// <summary>
    /// Checks whether a product batch number already exists.
    /// </summary>
    /// <param name="batchNumber">Batch number.</param>
    /// <param name="excludeBatchId">Optional batch identifier to exclude from check.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns><c>true</c> when batch number exists; otherwise <c>false</c>.</returns>
    Task<bool> BatchNumberExistsAsync(string batchNumber, Guid? excludeBatchId = null, CancellationToken ct = default);

    /// <summary>
    /// Creates a product batch.
    /// </summary>
    /// <param name="dto">Product batch creation data.</param>
    /// <param name="createdBy">User performing operation.</param>
    /// <param name="ipAddress">Optional client IP address.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Created product batch aggregate.</returns>
    Task<ProductBatch> CreateAsync(CreateProductBatchDto dto, UserSnapshot createdBy, string? ipAddress = null, CancellationToken ct = default);

    /// <summary>
    /// Updates a product batch.
    /// </summary>
    /// <param name="productId">Product identifier from route context.</param>
    /// <param name="batchId">Batch identifier.</param>
    /// <param name="dto">Product batch update data.</param>
    /// <param name="updatedBy">User performing operation.</param>
    /// <param name="ipAddress">Optional client IP address.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Updated product batch, or <c>null</c> if not found.</returns>
    Task<ProductBatch?> UpdateAsync(Guid productId, Guid batchId, UpdateProductBatchDto dto, UserSnapshot updatedBy, string? ipAddress = null, CancellationToken ct = default);

    /// <summary>
    /// Deletes a product batch.
    /// </summary>
    /// <param name="batchId">Batch identifier.</param>
    /// <param name="deletedBy">User performing operation.</param>
    /// <param name="ipAddress">Optional client IP address.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns><c>true</c> when deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeleteAsync(Guid batchId, UserSnapshot deletedBy, string? ipAddress = null, CancellationToken ct = default);
}
