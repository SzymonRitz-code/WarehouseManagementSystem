namespace WarehouseManagementSystem.API.Services.AuditLogs.Command
{
    /// <summary>
    /// Defines operations for writing audit log entries.
    /// </summary>
    public interface IAuditLogCommandService
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
}
