using System.Text.Json;
using System.Text.Json.Serialization;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.AuditDomain;

namespace WarehouseManagementSystem.API.Services.AuditLogs.Command
{

    public class AuditLogCommandService : IAuditLogCommandService
    {
        #region Fields and Constructor

        private const int MaxAuditValueLength = 500;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        private readonly IUnitOfWork _unitOfWork;

        public AuditLogCommandService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #endregion

        #region Audit Log Operations

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
            ct.ThrowIfCancellationRequested();
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
            ct.ThrowIfCancellationRequested();
            var oldValues = oldSnapshot;
            var newValues = newSnapshot;

            if (oldSnapshot is not null && newSnapshot is not null)
            {
                var (OldValues, NewValues) = BuildChangeSet(oldSnapshot, newSnapshot);
                oldValues = OldValues;
                newValues = NewValues;

                if (OldValues.Count == 0 && NewValues.Count == 0)
                {
                    return;
                }
            }

            AddLog(entityName, entityId, operation, performedById, oldValues, newValues, ipAddress);
            await Task.CompletedTask;
        }

        #endregion

        #region Helper Methods

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
                OldValues = SerializeAuditValues(oldValues),
                NewValues = SerializeAuditValues(newValues),
                PerformedAt = DateTimeOffset.UtcNow,
                PerformedById = performedById,
                IpAddress = ipAddress
            };

            _unitOfWork.AuditLogs.Add(log);
        }

        private static string SerializeAuditValues(object? values)
        {
            if (values is null)
            {
                return string.Empty;
            }

            var json = JsonSerializer.Serialize(values, JsonOptions);
            return json.Length <= MaxAuditValueLength
                ? json
                : string.Concat(json.AsSpan(0, MaxAuditValueLength - 3), "...");
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
                {
                    continue;
                }

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

        #endregion
    }
}
