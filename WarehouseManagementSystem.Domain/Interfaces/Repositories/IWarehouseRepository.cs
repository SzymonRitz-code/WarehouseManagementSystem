using WarehouseManagementSystem.Domain.Interfaces.Repositories.Base;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

namespace WarehouseManagementSystem.Domain.Interfaces.Repositories;

public interface IWarehouseRepository : IRepository<Warehouse>
{
    Task<IEnumerable<Warehouse>> AllAsync();
}