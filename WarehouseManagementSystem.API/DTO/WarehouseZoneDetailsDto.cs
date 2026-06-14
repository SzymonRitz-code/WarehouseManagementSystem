using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.API.DTO;

public class WarehouseZoneDetailsDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TemperatureType TemperatureType { get; set; }
    public bool IsPickingZone { get; set; }
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
}
