using WarehouseManagementSystem.Domain.Interfaces.Repositories.Base;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.Domain.Interfaces.Repositories;

public interface IProductBatchRepository : IRepository<ProductBatch>
{
    IEnumerable<ProductBatch> All();
    Task<IEnumerable<ProductBatch>> AllAsync();
}