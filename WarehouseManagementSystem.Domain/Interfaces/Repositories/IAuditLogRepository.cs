using WarehouseManagementSystem.Domain.Interfaces.Repositories.Base;
using WarehouseManagementSystem.Domain.Model.AuditDomain;

namespace WarehouseManagementSystem.Domain.Interfaces.Repositories;

public interface IAuditLogRepository : IReadOnlyRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetFilteredAsync(
        string? entityName,
        Guid? entityId,
        Guid? performedById);
}