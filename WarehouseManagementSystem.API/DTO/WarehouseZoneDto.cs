using System.ComponentModel.DataAnnotations;
using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.API.DTO
{
    public record struct WarehouseZoneDto(
        [property: Required] Guid Id,

        [property: Required, StringLength(30)]
        string Code,

        [property: Required, StringLength(200)]
        string Name,

        [property: Required]
        TemperatureType TemperatureType, // enum zgodny z Rich model

        [property: Required]
        bool IsPickingZone,

        [property: Required]
        DateTimeOffset CreatedAt,

        [property: Required]
        Guid WarehouseId,

        [property: Required, StringLength(200)]
        string WarehouseName
    );
}
