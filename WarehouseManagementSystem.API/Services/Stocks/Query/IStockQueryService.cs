using WarehouseManagementSystem.API.DTO;

namespace WarehouseManagementSystem.API.Services.Stocks.Query;

/// <summary>
/// Defines stock read operations for API read models.
/// </summary>
public interface IStockQueryService
{
    Task<PagedResult<StockDto>> GetStocksAsync(StockListQuery query, CancellationToken ct = default);
    Task<List<StockDto>> GetStockAvailabilityAsync(CancellationToken ct = default);
    Task<StockDto?> GetStockDetailsAsync(Guid stockId, CancellationToken ct = default);
    Task<IReadOnlyList<StockDto>> GetProductStocksAsync(Guid productId, CancellationToken ct = default);
    Task<IReadOnlyList<StockDto>> GetByWarehouseAsync(Guid warehouseId, CancellationToken ct = default);
    Task<IReadOnlyList<StockDto>> GetAvailableForPickingAsync(Guid warehouseId, CancellationToken ct = default);
    Task<decimal> GetAvailableQuantityAsync(Guid productId, Guid? productBatchId, Guid warehouseId, Guid? warehouseZoneId, CancellationToken ct = default);
    Task<IReadOnlyList<StockReservationDto>> GetReservationsAsync(Guid stockId, CancellationToken ct = default);
    Task<StockReservationDto?> GetReservationAsync(Guid stockId, Guid reservationId, CancellationToken ct = default);
}
