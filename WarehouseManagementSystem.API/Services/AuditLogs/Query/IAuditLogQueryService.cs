using WarehouseManagementSystem.Domain.Model.AuditDomain;

namespace WarehouseManagementSystem.API.Services.AuditLogs.Query;

/// <summary>
/// Defines read operations for audit logs.
/// </summary>
public interface IAuditLogQueryService
{
    /// <summary>
    /// Gets audit log entries with optional filtering.
    /// </summary>
    /// <param name="entityName">Optional entity name filter.</param>
    /// <param name="entityId">Optional entity identifier filter.</param>
    /// <param name="performedById">Optional user identifier filter.</param>
    /// <returns>Audit logs matching provided filters.</returns>
    Task<IEnumerable<AuditLog>> GetFilteredAsync(
        string? entityName,
        Guid? entityId,
        Guid? performedById);

    /// <summary>
    /// Gets audit log by identifier.
    /// </summary>
    /// <param name="id">Audit log identifier.</param>
    /// <returns>Audit log, or <c>null</c> when not found.</returns>
    Task<AuditLog?> GetByIdAsync(Guid id);
}
