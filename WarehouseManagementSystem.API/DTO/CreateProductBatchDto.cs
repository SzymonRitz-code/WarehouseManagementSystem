using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.API.DTO;

public class CreateProductBatchDto
{
    [Required, StringLength(50)]
    public string BatchNumber { get; set; }
    public Guid ProductId { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public DateOnly? ManufacturedDate { get; set; }
}

