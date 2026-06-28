using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Services.Warehouses.Command;

/// <summary>
/// Defines warehouse zone operations that change state.
/// </summary>
public interface IWarehouseZoneCommandService
{
    /// <summary>
    /// Checks whether a warehouse zone with the specified code exists.
    /// </summary>
    /// <param name="code">Warehouse zone code.</param>
    /// <param name="excludeWarehouseZoneId">Optional warehouse zone identifier to exclude from check.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns><c>true</c> when warehouse zone code exists; otherwise <c>false</c>.</returns>
    Task<bool> CodeExistsAsync(string code, Guid? excludeWarehouseZoneId = null, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a warehouse zone with the specified identifier exists.
    /// </summary>
    /// <param name="warehouseZoneId">Warehouse zone identifier.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns><c>true</c> if warehouse zone exists; otherwise <c>false</c>.</returns>
    Task<bool> ExistsAsync(Guid warehouseZoneId, CancellationToken ct = default);

    /// <summary>
    /// Creates a warehouse zone.
    /// </summary>
    /// <param name="dto">Warehouse zone creation data.</param>
    /// <param name="createdBy">User performing operation.</param>
    /// <param name="ipAddress">Optional client IP address.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Created warehouse zone aggregate.</returns>
    Task<WarehouseZone> CreateAsync(
        CreateWarehouseZoneDto dto,
        UserSnapshot createdBy,
        string? ipAddress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates a warehouse zone.
    /// </summary>
    /// <param name="warehouseZoneId">Warehouse zone identifier.</param>
    /// <param name="dto">Warehouse zone update data.</param>
    /// <param name="updatedBy">User performing operation.</param>
    /// <param name="ipAddress">Optional client IP address.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Updated warehouse zone, or <c>null</c> if not found.</returns>
    Task<WarehouseZone?> UpdateAsync(
        Guid warehouseZoneId,
        UpdateWarehouseZoneDto dto,
        UserSnapshot updatedBy,
        string? ipAddress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a warehouse zone.
    /// </summary>
    /// <param name="warehouseZoneId">Warehouse zone identifier.</param>
    /// <param name="deletedBy">User performing operation.</param>
    /// <param name="ipAddress">Optional client IP address.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns><c>true</c> when deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeleteAsync(
        Guid warehouseZoneId,
        UserSnapshot deletedBy,
        string? ipAddress = null,
        CancellationToken ct = default);
}
