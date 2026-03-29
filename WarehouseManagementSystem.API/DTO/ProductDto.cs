using System.ComponentModel.DataAnnotations;
 
namespace WarehouseManagementSystem.API.DTO;
public class ProductDto: CreateProductDto
{
    public Guid Id { get; set; } 

    [property: Required]
    public DateTimeOffset CreatedAt { get; set; }
}
