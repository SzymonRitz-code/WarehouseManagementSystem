using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.API.DTO;

public record struct WarehouseZoneListDto(
    Guid Id,
    string Code,
    string Name,
    TemperatureType TemperatureType,
    bool IsPickingZone,
    Guid WarehouseId,
    string? WarehouseName,
    decimal StockQty,
    DateTimeOffset CreatedAt);
