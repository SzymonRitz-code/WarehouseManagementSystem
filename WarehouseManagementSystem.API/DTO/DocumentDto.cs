using System.ComponentModel.DataAnnotations;
using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.API.DTO;

public class DocumentDto : CreateDocumentDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public DocumentStatus Status { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ConfirmedAt { get; set; }

    [Required]
    public Guid CreatedById { get; set; }

    [Required, StringLength(200)]
    public string? CreatedByName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string? CreatedByEmail { get; set; } = string.Empty;

    public Guid? ConfirmedById { get; set; }

    [StringLength(200)]
    public string? ConfirmedByName { get; set; }

    [EmailAddress, StringLength(255)]
    public string? ConfirmedByEmail { get; set; }
    [StringLength(200)]
    public string? SourceWarehouseName { get; set; }

    [StringLength(200)]
    public string? TargetWarehouseName { get; set; }
}