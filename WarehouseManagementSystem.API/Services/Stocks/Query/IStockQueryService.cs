using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.API.Services.Stocks.Query;

/// <summary>
/// Defines stock read operations.
/// </summary>
public interface IStockQueryService
{
    #region Stock DTO Queries

    /// <summary>
    /// Gets a paginated list of stock records using the provided filters.
    /// </summary>
    /// <param name="query">Filtering, sorting, and pagination parameters for stock records.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Paginated stock record list.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<PagedResult<StockDto>> GetStocksAsync(StockListQuery query, CancellationToken ct = default);

    /// <summary>
    /// Gets stock records with calculated availability.
    /// </summary>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>List of stock records with total, reserved, and available quantities.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<List<StockDto>> GetStockAvailabilityAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets stock record details by identifier.
    /// </summary>
    /// <param name="stockId">Stock record identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Stock record details, or <c>null</c> if the stock record does not exist.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<StockDto?> GetStockDetailsAsync(Guid stockId, CancellationToken ct = default);

    /// <summary>
    /// Gets stock records for the selected product as list DTOs.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>List of stock records for the selected product.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IReadOnlyList<StockDto>> GetProductStocksAsync(Guid productId, CancellationToken ct = default);

    #endregion

    #region Stock Lookup Queries

    /// <summary>
    /// Gets a stock aggregate by identifier.
    /// </summary>
    /// <param name="stockId">Stock record identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Stock record, or <c>null</c> if it does not exist.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<Stock?> GetByIdAsync(Guid stockId, CancellationToken ct = default);

    /// <summary>
    /// Gets a stock record for a product in a specific location.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="productBatchId">Optional product batch identifier.</param>
    /// <param name="warehouseId">Warehouse identifier.</param>
    /// <param name="warehouseZoneId">Optional warehouse zone identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Stock record matching the parameters, or <c>null</c> if it does not exist.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<Stock?> GetStockAsync(
        Guid productId,
        Guid? productBatchId,
        Guid warehouseId,
        Guid? warehouseZoneId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all stock records for a product.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>List of product stock records.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IReadOnlyList<Stock>> GetByProductAsync(
        Guid productId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets product stock records in the selected warehouse.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="warehouseId">Warehouse identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>List of product stock records in the warehouse.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IReadOnlyList<Stock>> GetByProductAndWarehouseAsync(
        Guid productId,
        Guid warehouseId,
        CancellationToken ct = default);

    #endregion

    #region Quantity and Classification Queries

    /// <summary>
    /// Gets the available, unreserved product quantity in a warehouse and optionally in a batch or zone.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="productBatchId">Optional product batch identifier.</param>
    /// <param name="warehouseId">Warehouse identifier.</param>
    /// <param name="warehouseZoneId">Optional warehouse zone identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Available product quantity. Returns <c>0</c> when no stock record is found.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<decimal> GetAvailableQuantityAsync(
        Guid productId,
        Guid? productBatchId,
        Guid warehouseId,
        Guid? warehouseZoneId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the total product quantity in a warehouse.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="warehouseId">Warehouse identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Total product quantity in the warehouse. Returns <c>0</c> when no stock record is found.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<decimal> GetTotalQuantityAsync(
        Guid productId,
        Guid warehouseId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets stock records located in zones with the specified temperature type.
    /// </summary>
    /// <param name="temperatureType">Warehouse zone temperature type.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>List of stock records from matching zones.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IReadOnlyList<Stock>> GetByTemperatureAsync(
        TemperatureType temperatureType,
        CancellationToken ct = default);

    #endregion

    #region Warehouse Location Queries

    /// <summary>
    /// Gets stock records for the selected warehouse.
    /// </summary>
    /// <param name="warehouseId">Warehouse identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>List of stock records in the warehouse.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IReadOnlyList<Stock>> GetByWarehouseAsync(Guid warehouseId, CancellationToken ct = default);

    /// <summary>
    /// Gets stock records in a specific warehouse zone.
    /// </summary>
    /// <param name="warehouseZoneId">Warehouse zone identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>List of stock records in the zone.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IReadOnlyList<Stock>> GetByWarehouseZoneAsync(
        Guid warehouseZoneId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets stock records available for picking in a warehouse.
    /// </summary>
    /// <param name="warehouseId">Warehouse identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>List of stock records with positive available quantity.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IReadOnlyList<Stock>> GetAvailableForPickingAsync(
        Guid warehouseId,
        CancellationToken ct = default);

    #endregion
}
