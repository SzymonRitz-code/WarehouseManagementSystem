using WarehouseManagementSystem.Domain.Model.CatalogDomain;

namespace WarehouseManagementSystem.Domain.Model.InventoryDomain;

public class ProductBatch
{
    public Guid Id { get; set; }
    public string BatchNumber  { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public DateOnly? ManufacturedDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Guid ProductId { get; set; }

    public virtual Product Product { get; set; }
}
