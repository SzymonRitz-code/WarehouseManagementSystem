using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.API.Services.Queries;

public interface IStockQueryService
{
    /// <summary>
    /// Pobiera stan magazynowy po Id.
    /// </summary>
    Task<Stock?> GetByIdAsync(Guid stockId, CancellationToken ct = default);

    /// <summary>
    /// Pobiera stan magazynowy dla produktu w konkretnej lokalizacji.
    /// </summary>
    Task<Stock?> GetStockAsync(
        Guid productId,
        Guid? productBatchId,
        Guid warehouseId,
        Guid? warehouseZoneId,
        CancellationToken ct = default);

    /// <summary>
    /// Pobiera wszystkie stany magazynowe dla produktu.
    /// </summary>
    Task<IReadOnlyList<Stock>> GetByProductAsync(
        Guid productId,
        CancellationToken ct = default);

    /// <summary>
    /// Pobiera stany magazynowe dla produktu w danym magazynie.
    /// </summary>
    Task<IReadOnlyList<Stock>> GetByProductAndWarehouseAsync(
        Guid productId,
        Guid warehouseId,
        CancellationToken ct = default);

    /// <summary>
    /// Pobiera dostępny (niezarezerwowany) stan dla produktu.
    /// </summary>
    Task<decimal> GetAvailableQuantityAsync(
        Guid productId,
        Guid? productBatchId,
        Guid warehouseId,
        Guid? warehouseZoneId,
        CancellationToken ct = default);

    /// <summary>
    /// Pobiera całkowitą ilość produktu w magazynie.
    /// </summary>
    Task<decimal> GetTotalQuantityAsync(
        Guid productId,
        Guid warehouseId,
        CancellationToken ct = default);


    /// <summary>
    /// Pobiera stany magazynowe wg typu temperatury.
    /// </summary>
    Task<IReadOnlyList<Stock>> GetByTemperatureAsync(
        TemperatureType temperatureType,
        CancellationToken ct = default);

    Task<IReadOnlyList<Stock>> GetByWarehouseAsync(Guid warehouseId, CancellationToken ct = default);

    /// <summary>
    /// Pobiera stany magazynowe w konkretnej strefie magazynowej.
    /// </summary>
    Task<IReadOnlyList<Stock>> GetByZoneAsync(
        Guid warehouseZoneId,
        CancellationToken ct = default);

    /// <summary>
    /// Pobiera produkty dostępne do kompletacji (posiadają dostępny stan).
    /// </summary>
    Task<IReadOnlyList<Stock>> GetAvailableForPickingAsync(
        Guid warehouseId,
        CancellationToken ct = default);
}
