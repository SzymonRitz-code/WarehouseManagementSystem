namespace WarehouseManagementSystem.Domain.Model.CatalogDomain;

public class Product
{
    public Guid Id { get; set; }
    public string SKU { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Unit { get; set; }
    public bool RequiresBatch { get; set; }
    public bool IsActive { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Volume { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
