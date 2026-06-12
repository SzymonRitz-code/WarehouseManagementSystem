using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.API.DTO;

public class UpdateProductBatchDto
{
    public Guid Id { get; set; }

    [Required, StringLength(50)]
    public string BatchNumber { get; set; } = string.Empty;

    public DateOnly? ExpirationDate { get; set; }

    public DateOnly? ManufacturedDate { get; set; }
}
