using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.Domain.Services;

public interface IStockReservationService
{
    Task ExpireReservationsAsync();
}
