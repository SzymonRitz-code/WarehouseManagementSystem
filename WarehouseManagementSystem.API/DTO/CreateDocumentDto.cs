using System.ComponentModel.DataAnnotations;
using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.API.DTO;

public record struct CreateDocumentDto(
    [Required]
    DocumentType Type,
    [Required]
    Guid CreatedById,
    [Required]
    Guid SourceWarehouseId,
    Guid? TargetWarehouseId,
    [Required]
    DateTime DocumentDate,
    [StringLength(1000)]
    string? Notes,
    [Required, MinLength(1, ErrorMessage = "Document must have at least one item.")]
    List<DocumentItemDto> Items
);