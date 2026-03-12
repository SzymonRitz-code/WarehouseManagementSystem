using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.API.DTO
{
    public record struct WarehouseDto(
        [property: Required] Guid Id,

        [property: Required, StringLength(30)]
        string Code,

        [property: Required, StringLength(200)]
        string Name,

        [property: Required, StringLength(200)]
        string Country,

        [property: Required, StringLength(200)]
        string City,

        [property: Required, StringLength(200)]
        string Address,

        [property: Required]
        bool IsActive,

        [property: Required]
        DateTimeOffset CreatedAt
    );
}
