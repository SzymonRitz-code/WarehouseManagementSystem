using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.API.DTO;

public class DocumentListDto
{
    public string? DocumentNumber { get; set; }
    public DocumentType Type { get; set; }
    public DocumentStatus Status { get; set; }
    public string SourceWarehouse { get; set; }
    public string? TargetWarehouse { get; set; }
    public string CreatedBy { get; set; }
    public string ConfirmedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public Guid Id { get; internal set; }
}
