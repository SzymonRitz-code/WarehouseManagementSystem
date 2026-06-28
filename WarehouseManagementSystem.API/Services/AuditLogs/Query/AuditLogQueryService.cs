using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.AuditDomain;

namespace WarehouseManagementSystem.API.Services.AuditLogs.Query;

public class AuditLogQueryService : IAuditLogQueryService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditLogQueryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<AuditLog>> GetFilteredAsync(
        string? entityName,
        Guid? entityId,
        Guid? performedById)
    {
        return await _unitOfWork.AuditLogs.GetFilteredAsync(entityName, entityId, performedById);
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id)
    {
        return await _unitOfWork.AuditLogs.FindAsync(id);
    }
}
