using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.API.DTO;

public record struct AuditLogDto(
    [property: Required] Guid Id,

    [property: Required, StringLength(200)]
    string EntityName,

    [property: Required] Guid EntityId,

    [property: Required, StringLength(50)]
    string Operation,

    string? OldValues,

    string? NewValues,

    [property: Required]
    DateTimeOffset PerformedAt,

    [property: StringLength(50)]
    string? IpAddress,

    [property: Required]
    Guid PerformedById,

    [property: Required, StringLength(200)]
    string PerformedByName,

    [property: Required, EmailAddress, StringLength(255)]
    string PerformedByEmail
);
