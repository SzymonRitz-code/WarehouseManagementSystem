using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.API.DTO;

public class ProductDetailsDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public UnitOfMeasure Unit { get; set; }
    public bool RequiresBatch { get; set; }
    public bool IsActive { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Volume { get; set; }
}
