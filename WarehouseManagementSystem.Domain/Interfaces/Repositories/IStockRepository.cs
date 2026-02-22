using WarehouseManagementSystem.Domain.Interfaces.Repositories.Base;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.Domain.Interfaces.Repositories;

public interface IStockRepository : IRepository<Stock>
{
    Task<IEnumerable<Stock>> All();
    Task<Stock?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, Guid? batchId);
}