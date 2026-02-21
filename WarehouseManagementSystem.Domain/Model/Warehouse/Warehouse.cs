using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.Domain.Model.WarehouseDomain;

public class Warehouse
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Country { get; set; }
    public string City { get; set; }
    public string Address { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public virtual ICollection<Document> SourceDocuments { get; set; }
    public virtual ICollection<Document> TargetDocuments { get; set; }
    public virtual ICollection<Stock> Stocks { get; set; }
    public virtual ICollection<WarehouseZone> Zones { get; set; }
}
