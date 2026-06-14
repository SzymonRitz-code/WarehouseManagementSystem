namespace WarehouseManagementSystem.API.DTO;

public class ProductBatchDto
{
    public Guid Id { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public DateOnly? ManufacturedDate { get; set; }
    public string? ProductName { get; set; }
}
