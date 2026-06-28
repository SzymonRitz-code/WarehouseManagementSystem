using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.API.Services.Warehouses.Command;

/// <summary>
/// Defines warehouse operations that change state.
/// </summary>
public interface IWarehouseCommandService
{
    /// <summary>
    /// Checks whether a warehouse with the specified code exists.
    /// </summary>
    /// <param name="code">Warehouse code.</param>
    /// <param name="excludeWarehouseId">Optional warehouse identifier to exclude from check.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns><c>true</c> when warehouse code exists; otherwise <c>false</c>.</returns>
    bool CodeExists(string code, Guid? excludeWarehouseId = null, CancellationToken ct = default);

    /// <summary>
    /// Creates a warehouse.
    /// </summary>
    /// <param name="dto">Warehouse creation data.</param>
    /// <param name="createdBy">User performing operation.</param>
    /// <param name="ipAddress">Optional client IP address.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Created warehouse aggregate.</returns>
    Task<Warehouse> CreateAsync(
        CreateWarehouseDto dto,
        UserSnapshot createdBy,
        string? ipAddress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates a warehouse.
    /// </summary>
    /// <param name="warehouseId">Warehouse identifier.</param>
    /// <param name="dto">Warehouse update data.</param>
    /// <param name="updatedBy">User performing operation.</param>
    /// <param name="ipAddress">Optional client IP address.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>Updated warehouse, or <c>null</c> if not found.</returns>
    Task<Warehouse?> UpdateAsync(
        Guid warehouseId,
        UpdateWarehouseDto dto,
        UserSnapshot updatedBy,
        string? ipAddress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a warehouse.
    /// </summary>
    /// <param name="warehouseId">Warehouse identifier.</param>
    /// <param name="deletedBy">User performing operation.</param>
    /// <param name="ipAddress">Optional client IP address.</param>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns><c>true</c> when deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeleteAsync(
        Guid warehouseId,
        UserSnapshot deletedBy,
        string? ipAddress = null,
        CancellationToken ct = default);
}
