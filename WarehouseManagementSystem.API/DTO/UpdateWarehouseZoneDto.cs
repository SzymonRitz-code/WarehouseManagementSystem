using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.API.DTO;

public class UpdateWarehouseZoneDto
{
    public Guid Id { get; set; }

    [Required, StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TemperatureType TemperatureType { get; set; }

    [Required]
    public bool IsPickingZone { get; set; }

    [Required]
    public Guid WarehouseId { get; set; }
}
