using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.Domain.Services;

public interface IStockService
{
    Task<Stock> GetOrCreateAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, Guid? batchId);

    Task IncreaseStockAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, decimal quantity, Guid? batchId);

    Task DecreaseStockAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, decimal quantity, Guid? batchId);

    Task<StockReservation> ReserveStockAsync(
        Guid stockId,
        decimal quantity,
        string reservationSource,
        Guid createdBy,
        DateTimeOffset? expiresAt = null);

    Task ReleaseReservationAsync(Guid stockId, Guid reservationId);

    Task CancelReservationAsync(Guid reservationId);

    Task ConfirmReservationAsync(Guid reservationId);

    Task ExpireReservationsAsync();

    Task MoveStockAsync(Guid productId, Guid sourceWarehouseId, Guid sourceZoneId, Guid targetWarehouseId, Guid targetZoneId, decimal quantity, Guid? batchId);
}
