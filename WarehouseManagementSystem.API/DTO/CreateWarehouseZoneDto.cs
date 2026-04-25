using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.API.DTO;

public class CreateWarehouseZoneDto
{
    [Required, StringLength(30)]
    public string Code { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TemperatureType TemperatureType { get; set; } // enum zgodny z Rich model

    [Required]
    public bool IsPickingZone { get; set; }

    [Required]
    public Guid WarehouseId { get; set; }


}
