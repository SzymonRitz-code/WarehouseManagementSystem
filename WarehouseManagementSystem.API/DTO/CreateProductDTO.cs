using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.API.DTO
{
    public record struct CreateProductDTO(
    [Required(ErrorMessage = "Product name is required.")]
    [MaxLength(200, ErrorMessage = "Product name cannot exceed 200 characters.")]
    string Name,

    [Required(ErrorMessage = "SKU is required.")]
    [MaxLength(50, ErrorMessage = "SKU cannot exceed 50 characters.")]
    string SKU
    );
}
