using WarehouseManagementSystem.API.DTO;

namespace WarehouseManagementSystem.API.Services.Queries;

public interface IWarehouseQueryService
{
    Task<IReadOnlyList<WarehouseListDto>> GetWarehousesAsync(CancellationToken ct = default);
    Task<WarehouseDetailsDto?> GetWarehouseAsync(Guid warehouseId, CancellationToken ct = default);
    Task<IReadOnlyList<WarehouseZoneListDto>> GetWarehouseZonesAsync(CancellationToken ct = default);
    Task<WarehouseZoneDetailsDto?> GetWarehouseZoneAsync(Guid warehouseZoneId, CancellationToken ct = default);
}
