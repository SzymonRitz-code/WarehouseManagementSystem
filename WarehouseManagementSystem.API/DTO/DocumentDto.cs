using System.ComponentModel.DataAnnotations;
using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.API.DTO;

public record struct DocumentDto(
    [property: Required] Guid Id,

    [property: Required, StringLength(50)]
    string Number,

    [property: Required]
    DateTime DocumentDate,

    [property: Required]
    DocumentType Type,

    [property: Required]
    DocumentStatus Status,

    [property: StringLength(1000)]
    string? Notes, 

    [property: Required]
    DateTimeOffset CreatedAt,

    DateTimeOffset? ConfirmedAt,

    [property: Required]
    Guid CreatedById,

    [property: Required, StringLength(200)]
    string CreatedByName,

    [property: Required, EmailAddress, StringLength(255)]
    string CreatedByEmail,

    Guid? ConfirmedById,

    [property: StringLength(200)]
    string? ConfirmedByName,

    [property: EmailAddress, StringLength(255)]
    string? ConfirmedByEmail,

    Guid? SourceWarehouseId,

    [property: StringLength(200)]
    string? SourceWarehouseName,

    Guid? TargetWarehouseId,

    [property: StringLength(200)]
    string? TargetWarehouseName
);