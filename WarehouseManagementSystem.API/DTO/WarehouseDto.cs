using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.API.DTO
{
    public class WarehouseDto : CreateWarehouseDto 
    {
        [property: Required]
        public Guid Id { get; set; }
        [property: Required]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
