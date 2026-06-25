using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.API.DTO;

public class DocumentDto
{
    public Guid Id { get; set; }
    public string? Number { get; set; }
    public DocumentType Type { get; set; }
    public DocumentStatus Status { get; set; }
    public DateTime DocumentDate { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? TransferStartedAt { get; set; }

    public Guid SourceWarehouseId { get; set; }
    public string? SourceWarehouseName { get; set; }
    public Guid? TargetWarehouseId { get; set; }
    public string? TargetWarehouseName { get; set; }

    public Guid CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public string? CreatedByEmail { get; set; }
    public Guid? ConfirmedById { get; set; }
    public string? ConfirmedByName { get; set; }
    public string? ConfirmedByEmail { get; set; }

    public List<DocumentItemDto> Items { get; set; } = [];
}
