using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.API.DTO;

public class UpdateWarehouseDto
{
    public Guid Id { get; set; }

    [Required, StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [Required]
    public bool IsActive { get; set; }
}
