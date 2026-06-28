using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Services.Products.Command;

/// <summary>
/// Defines operations that change product state.
/// </summary>
public interface IProductCommandService
{
    /// <summary>
    /// Checks whether a SKU already exists.
    /// </summary>
    /// <param name="sku">Product SKU.</param>
    /// <param name="excludeProductId">Optional product id to exclude from uniqueness check.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns><c>true</c> when SKU exists; otherwise <c>false</c>.</returns>
    bool SkuExists(string sku, Guid? excludeProductId = null, CancellationToken ct = default);

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="dto">Product data.</param>
    /// <param name="createdBy">User creating the product.</param>
    /// <param name="ipAddress">Optional client IP address.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>The created product aggregate.</returns>
    Task<Product> CreateProductAsync(
        CreateProductDto dto,
        UserSnapshot createdBy,
        string? ipAddress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="dto">Updated product data.</param>
    /// <param name="updatedBy">User performing update.</param>
    /// <param name="ipAddress">Optional client IP address.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Updated product, or <c>null</c> if product does not exist.</returns>
    Task<Product?> UpdateProductAsync(
        Guid productId,
        UpdateProductDto dto,
        UserSnapshot updatedBy,
        string? ipAddress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes an existing product.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="deletedBy">User performing delete.</param>
    /// <param name="ipAddress">Optional client IP address.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns><c>true</c> if product was deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeleteProductAsync(
        Guid productId,
        UserSnapshot deletedBy,
        string? ipAddress = null,
        CancellationToken ct = default);
}
