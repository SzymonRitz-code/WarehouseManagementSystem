using System.Text.Json;
using System.Text.Json.Serialization;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.AuditDomain;

namespace WarehouseManagementSystem.API.Services.AuditLogs
{
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

        Task LogChangesAsync(
            string entityName,
            Guid entityId,
            string operation,
            Guid performedById,
            object? oldSnapshot,
            object? newSnapshot,
            string? ipAddress = null,
            CancellationToken ct = default);
    }

    public class AuditLogService : IAuditLogService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

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
            AddLog(entityName, entityId, operation, performedById, oldValues, newValues, ipAddress);
            await Task.CompletedTask;
        }

        public async Task LogChangesAsync(
            string entityName,
            Guid entityId,
            string operation,
            Guid performedById,
            object? oldSnapshot,
            object? newSnapshot,
            string? ipAddress = null,
            CancellationToken ct = default)
        {
            var oldValues = oldSnapshot;
            var newValues = newSnapshot;

            if (oldSnapshot is not null && newSnapshot is not null)
            {
                var changeSet = BuildChangeSet(oldSnapshot, newSnapshot);
                oldValues = changeSet.OldValues;
                newValues = changeSet.NewValues;

                if (changeSet.OldValues.Count == 0 && changeSet.NewValues.Count == 0)
                    return;
            }

            AddLog(entityName, entityId, operation, performedById, oldValues, newValues, ipAddress);
            await Task.CompletedTask;
        }

        private void AddLog(
            string entityName,
            Guid entityId,
            string operation,
            Guid performedById,
            object? oldValues,
            object? newValues,
            string? ipAddress)
        {
            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = entityName,
                EntityId = entityId,
                Operation = operation,
                OldValues = oldValues != null
                    ? JsonSerializer.Serialize(oldValues, JsonOptions)
                    : string.Empty,
                NewValues = newValues != null
                    ? JsonSerializer.Serialize(newValues, JsonOptions)
                    : string.Empty,
                PerformedAt = DateTimeOffset.UtcNow,
                PerformedById = performedById,
                IpAddress = ipAddress
            };

            _unitOfWork.AuditLogs.Add(log);
        }

        private static (Dictionary<string, object?> OldValues, Dictionary<string, object?> NewValues) BuildChangeSet(
            object oldSnapshot,
            object newSnapshot)
        {
            var oldValues = ToDictionary(oldSnapshot);
            var newValues = ToDictionary(newSnapshot);
            var changedOldValues = new Dictionary<string, object?>();
            var changedNewValues = new Dictionary<string, object?>();

            foreach (var key in oldValues.Keys.Union(newValues.Keys).OrderBy(k => k))
            {
                oldValues.TryGetValue(key, out var oldValue);
                newValues.TryGetValue(key, out var newValue);

                if (JsonSerializer.Serialize(oldValue, JsonOptions) == JsonSerializer.Serialize(newValue, JsonOptions))
                    continue;

                changedOldValues[key] = oldValue;
                changedNewValues[key] = newValue;
            }

            return (changedOldValues, changedNewValues);
        }

        private static Dictionary<string, object?> ToDictionary(object value)
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions)
                   ?? new Dictionary<string, object?>();
        }
    }
}
