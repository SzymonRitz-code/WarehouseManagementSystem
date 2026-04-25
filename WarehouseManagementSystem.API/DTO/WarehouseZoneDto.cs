using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.API.DTO;

public class WarehouseZoneDto : CreateWarehouseZoneDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    [StringLength(200)]
    public string? WarehouseName { get; set; }

}
