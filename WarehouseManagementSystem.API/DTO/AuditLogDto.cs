using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.API.DTO
{
    public record struct AuditLogDto(
        Guid Id,
        string EntityName,
        Guid EntityId,
        string Operation,
        string? OldValues,
        string? NewValues,
        DateTimeOffset PerformedAt,
        string? IpAddress,
        Guid PerformedById,
        string PerformedByName,
        string PerformedByEmail
    );
}
