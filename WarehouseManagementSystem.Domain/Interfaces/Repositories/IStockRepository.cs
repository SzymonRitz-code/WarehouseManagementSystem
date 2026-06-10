using WarehouseManagementSystem.Domain.Interfaces.Repositories.Base;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.Domain.Interfaces.Repositories;

public interface IStockRepository : IRepository<Stock>
{
    Task<IEnumerable<Stock>> AllAsNoTrackingAsync();
    Task<IEnumerable<Stock>> All();
    Task<Stock?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, Guid? batchId);
    Task<Stock?> GetByProductAndWarehouseAsNoTrackingAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, Guid? batchId);
    Task<IReadOnlyList<StockReservation>> GetActiveReservationsAsync(Guid stockId);
    Task<IReadOnlyList<StockReservation>> GetExpiredReservationsAsync(DateTimeOffset currentTime);
    Task<IReadOnlyList<StockReservation>> GetActiveReservationsByDocumentIdAsync(Guid documentId);
    Task<IReadOnlyList<StockReservation>> FindReservationsByStockIdAsync(Guid stockId);
}