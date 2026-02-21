using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.SecurityDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

namespace WarehouseManagementSystem.Domain.Model.DocumentsDomain;

public class Document
{
    public Guid Id { get; set; }
    public string Number { get; set; }
    public DateTime DocumentDate { get; set; }
    public DocumentType Type { get; set; } // PZ, WZ, MM, ADJ
    public DocumentStatus Status { get; set; } // Draft, Confirmed, Cancelled
    public byte[] RowVersion { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }


    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; }

    public Guid? ConfirmedById { get; set; }
    public User? ConfirmedBy { get; set; }

    public Guid? SourceWarehouseId { get; set; }
    public Warehouse SourceWarehouse { get; set; }

    public Guid? TargetWarehouseId { get; set; }
    public Warehouse TargetWarehouse { get; set; }

    public ICollection<DocumentItem> Items { get; set; }

}
