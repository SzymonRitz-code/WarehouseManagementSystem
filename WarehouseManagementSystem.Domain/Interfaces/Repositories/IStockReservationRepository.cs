using WarehouseManagementSystem.Domain.Interfaces.Repositories.Base;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.Domain.Interfaces.Repositories;

public interface IStockReservationRepository : IRepository<StockReservation>
{
    Task<IEnumerable<StockReservation>> AllAsync();
    Task<IEnumerable<StockReservation>> GetActiveReservationsAsync(Guid stockId);
    Task<IReadOnlyCollection<StockReservation>> GetExpiredReservationsAsync(DateTimeOffset currentTime);
    Task<IReadOnlyCollection<StockReservation>> GetActiveReservationsByDocumentIdAsync(Guid documentId);
}