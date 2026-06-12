using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.API.DTO;

public class DocumentItemDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public Guid DocumentId { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Quantity { get; set; }

    public Guid? ProductBatchId { get; set; }

    public Guid? SourceZoneId { get; set; }

    public Guid? TargetZoneId { get; set; }

    [StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? ProductBatchNumber { get; set; }

    [StringLength(200)]
    public string? SourceZoneName { get; set; }

    [StringLength(200)]
    public string? TargetZoneName { get; set; }
}

public class DocumentItemCommandDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }

    public Guid? ProductBatchId { get; set; }

    public Guid? SourceZoneId { get; set; }

    public Guid? TargetZoneId { get; set; }
}
