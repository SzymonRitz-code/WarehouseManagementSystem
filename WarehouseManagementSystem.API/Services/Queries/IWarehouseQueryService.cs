using WarehouseManagementSystem.API.DTO;

namespace WarehouseManagementSystem.API.Services.Queries;

/// <summary>
/// Defines warehouse and warehouse zone read operations.
/// </summary>
public interface IWarehouseQueryService
{
    /// <summary>
    /// Gets warehouses for the list view.
    /// </summary>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Warehouse list.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IReadOnlyList<WarehouseListDto>> GetWarehousesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets warehouse details by identifier.
    /// </summary>
    /// <param name="warehouseId">Warehouse identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Warehouse details, or <c>null</c> if the warehouse does not exist.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<WarehouseDetailsDto?> GetWarehouseAsync(Guid warehouseId, CancellationToken ct = default);

    /// <summary>
    /// Gets warehouse zones for the list view.
    /// </summary>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Warehouse zone list.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<IReadOnlyList<WarehouseZoneListDto>> GetWarehouseZonesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets warehouse zone details by identifier.
    /// </summary>
    /// <param name="warehouseZoneId">Warehouse zone identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Warehouse zone details, or <c>null</c> if the zone does not exist.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="ct"/>.</exception>
    Task<WarehouseZoneDetailsDto?> GetWarehouseZoneAsync(Guid warehouseZoneId, CancellationToken ct = default);
}
