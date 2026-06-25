using System.Text.Json;
using System.Text.Json.Serialization;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.AuditDomain;

namespace WarehouseManagementSystem.API.Services.AuditLogs
{
    /// <summary>
    /// Defines operations for writing audit log entries.
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>
        /// Adds an audit log entry for a performed operation.
        /// </summary>
        /// <param name="entityName">Name of the entity the audit log entry applies to.</param>
        /// <param name="entityId">Identifier of the entity the audit log entry applies to.</param>
        /// <param name="operation">Name of the performed operation.</param>
        /// <param name="performedById">Identifier of the user who performed the operation.</param>
        /// <param name="oldValues">Optional previous entity state.</param>
        /// <param name="newValues">Optional new entity state.</param>
        /// <param name="ipAddress">Optional client IP address.</param>
        /// <param name="ct">Operation cancellation token.</param>
        /// <returns>A task representing the audit log creation operation.</returns>
        /// <exception cref="NotSupportedException">Thrown when the provided values cannot be serialized to JSON.</exception>
        Task LogAsync(
            string entityName,
            Guid entityId,
            string operation,
            Guid performedById,
            object? oldValues = null,
            object? newValues = null,
            string? ipAddress = null,
            CancellationToken ct = default);

        /// <summary>
        /// Adds an audit log entry containing differences between the previous and new entity snapshots.
        /// </summary>
        /// <param name="entityName">Name of the entity the audit log entry applies to.</param>
        /// <param name="entityId">Identifier of the entity the audit log entry applies to.</param>
        /// <param name="operation">Name of the performed operation.</param>
        /// <param name="performedById">Identifier of the user who performed the operation.</param>
        /// <param name="oldSnapshot">Optional previous entity snapshot.</param>
        /// <param name="newSnapshot">Optional new entity snapshot.</param>
        /// <param name="ipAddress">Optional client IP address.</param>
        /// <param name="ct">Operation cancellation token.</param>
        /// <returns>A task representing the audit log creation operation. If snapshots do not differ, no entry is created.</returns>
        /// <exception cref="NotSupportedException">Thrown when the provided snapshots cannot be serialized to JSON.</exception>
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
        #region Fields and Constructor

        private const int MaxAuditValueLength = 500;

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
