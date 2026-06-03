using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.AuditDomain;

namespace WarehouseManagementSystem.API.Services.AuditLogs
{
    // API/Services/Audit/AuditLogService.cs
    public interface IAuditLogService
    {
        Task LogAsync(
            string entityName,
            Guid entityId,
            string operation,
            Guid performedById,
            object? oldValues = null,
            object? newValues = null,
            string? ipAddress = null,
            CancellationToken ct = default);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuditLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task LogAsync(
            string entityName,
            Guid entityId,
            string operation,
            Guid performedById,
            object? oldValues = null,
            object? newValues = null,
            string? ipAddress = null,
            CancellationToken ct = default)
        {
            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = entityName,
                EntityId = entityId,
                Operation = operation,
                OldValues = oldValues != null
                    ? System.Text.Json.JsonSerializer.Serialize(oldValues)
                    : string.Empty,
                NewValues = newValues != null
                    ? System.Text.Json.JsonSerializer.Serialize(newValues)
                    : string.Empty,
                PerformedAt = DateTimeOffset.UtcNow,
                PerformedById = performedById,
                IpAddress = ipAddress
            };

            _unitOfWork.AuditLogs.Add(log);
            // Nie wywołuje SaveChanges tutaj — 
            // zapis następuje razem z operacją domenową w tej samej transakcji
        }
    }
}
