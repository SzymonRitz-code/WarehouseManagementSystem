using WarehouseManagementSystem.Domain.Exceptions;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Services.Stocks.Command;

/// <summary>
/// Defines operations that change stock records and reservations.
/// </summary>
public interface IStockCommandService
{
    #region Stock Quantity Operations

    /// <summary>
    /// Gets an existing stock record for the product and location, or creates a new one with zero quantity.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="warehouseId">Warehouse identifier.</param>
    /// <param name="warehouseZoneId">Warehouse zone identifier.</param>
    /// <param name="batchId">Optional product batch identifier.</param>
    /// <returns>The existing or newly created stock record.</returns>
    Task<Stock> GetOrCreateAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, Guid? batchId, CancellationToken ct = default);

    /// <summary>
    /// Increases product quantity in the selected warehouse location.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="warehouseId">Warehouse identifier.</param>
    /// <param name="warehouseZoneId">Warehouse zone identifier.</param>
    /// <param name="quantity">Quantity to add.</param>
    /// <param name="batchId">Optional product batch identifier.</param>
    /// <returns>A task representing the stock increase operation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="quantity"/> is less than or equal to zero.</exception>
    Task IncreaseStockAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, decimal quantity, Guid? batchId, CancellationToken ct = default);

    /// <summary>
    /// Decreases product quantity in the selected warehouse location.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="warehouseId">Warehouse identifier.</param>
    /// <param name="warehouseZoneId">Warehouse zone identifier.</param>
    /// <param name="quantity">Quantity to subtract.</param>
    /// <param name="batchId">Optional product batch identifier.</param>
    /// <returns>A task representing the stock decrease operation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="quantity"/> is less than or equal to zero.</exception>
    /// <exception cref="InsufficientStockException">Thrown when the available quantity is lower than the quantity to subtract.</exception>
    Task DecreaseStockAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, decimal quantity, Guid? batchId, CancellationToken ct = default);

    #endregion

    #region Reservation Operations

    /// <summary>
    /// Reserves part of the available stock quantity.
    /// </summary>
    /// <param name="stockId">Stock record identifier.</param>
    /// <param name="quantity">Quantity to reserve.</param>
    /// <param name="reservationSource">Reservation source or reason.</param>
    /// <param name="createdBy">User creating the reservation.</param>
    /// <param name="expiresAt">Optional reservation expiration time.</param>
    /// <returns>The created stock reservation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="quantity"/> is less than or equal to zero.</exception>
    /// <exception cref="StockNotFoundException">Thrown when the stock record with the specified identifier does not exist.</exception>
    /// <exception cref="InsufficientStockException">Thrown when the available quantity is lower than the quantity to reserve.</exception>
    Task<StockReservation> ReserveStockAsync(
        Guid stockId,
        decimal quantity,
        string reservationSource,
        UserSnapshot createdBy,
        DateTimeOffset? expiresAt = null,
        CancellationToken ct = default);

    /// <summary>
    /// Releases the specified stock reservation.
    /// </summary>
    /// <param name="stockId">Stock record identifier.</param>
    /// <param name="reservationId">Reservation identifier.</param>
    /// <returns>A task representing the reservation release operation.</returns>
    /// <exception cref="StockNotFoundException">Thrown when the stock record with the specified identifier does not exist.</exception>
    /// <exception cref="ReservationNotFoundException">Thrown when the reservation with the specified identifier does not exist in the stock record.</exception>
    Task ReleaseReservationAsync(Guid stockId, Guid reservationId, CancellationToken ct = default);

    /// <summary>
    /// Cancels the specified reservation.
    /// </summary>
    /// <param name="reservationId">Reservation identifier.</param>
    /// <returns>A task representing the reservation cancellation operation.</returns>
    /// <exception cref="ReservationNotFoundException">Thrown when the reservation with the specified identifier does not exist.</exception>
    Task CancelReservationAsync(Guid reservationId, CancellationToken ct = default);

    /// <summary>
    /// Confirms the specified reservation.
    /// </summary>
    /// <param name="reservationId">Reservation identifier.</param>
    /// <returns>A task representing the reservation confirmation operation.</returns>
    /// <exception cref="ReservationNotFoundException">Thrown when the reservation with the specified identifier does not exist.</exception>
    /// <exception cref="InsufficientStockException">Thrown when the available quantity is lower than the reservation quantity.</exception>
    Task ConfirmReservationAsync(Guid reservationId, CancellationToken ct = default);

    /// <summary>
    /// Expires active reservations whose expiration time has passed.
    /// </summary>
    /// <returns>A task representing the reservation expiration operation.</returns>
    Task ExpireReservationsAsync(CancellationToken ct = default);

    #endregion

    #region Movement Operations

    /// <summary>
    /// Moves product quantity between warehouse locations.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="sourceWarehouseId">Source warehouse identifier.</param>
    /// <param name="sourceZoneId">Source zone identifier.</param>
    /// <param name="targetWarehouseId">Target warehouse identifier.</param>
    /// <param name="targetZoneId">Target zone identifier.</param>
    /// <param name="quantity">Quantity to move.</param>
    /// <param name="batchId">Optional product batch identifier.</param>
    /// <returns>A task representing the stock movement operation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="quantity"/> is less than or equal to zero.</exception>
    /// <exception cref="InsufficientStockException">Thrown when the available quantity in the source location is lower than the quantity to move.</exception>
    Task MoveStockAsync(Guid productId, Guid sourceWarehouseId, Guid sourceZoneId, Guid targetWarehouseId, Guid targetZoneId, decimal quantity, Guid? batchId, CancellationToken ct = default);

    #endregion
}
