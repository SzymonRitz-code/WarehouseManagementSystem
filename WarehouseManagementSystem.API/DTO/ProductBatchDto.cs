namespace WarehouseManagementSystem.API.DTO;

public class ProductBatchDto: CreateProductBatchDto
{
    public Guid Id { get; set; }
    public string? ProductName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

