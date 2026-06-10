
using WarehouseManagementSystem.Domain.Interfaces.Repositories.Base;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

namespace WarehouseManagementSystem.Domain.Interfaces.Repositories;

public interface IWarehouseZoneRepository : IRepository<WarehouseZone>
{
    Task<IEnumerable<WarehouseZone>> AllAsync();
}