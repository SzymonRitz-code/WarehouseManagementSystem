using System.ComponentModel.DataAnnotations;
using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.API.DTO;

public class CreateDocumentDto
{
    [StringLength(50)]
    public string? Number { get; set; }

    [Required]
    public DocumentType Type { get; set; }

    [Required]
    public Guid SourceWarehouseId { get; set; }

    public Guid? TargetWarehouseId { get; set; }

    [Required]
    public DateTime DocumentDate { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Required, MinLength(1, ErrorMessage = "Document must have at least one item.")]
    public virtual List<CreateDocumentItemDto> Items { get; set; } = [];
}