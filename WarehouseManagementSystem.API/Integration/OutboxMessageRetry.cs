using WarehouseManagementSystem.Infrastructure.Integration;

namespace WarehouseManagementSystem.API.Integration;

/// <summary>Centralizes the state transition used after an unsuccessful broker publication.</summary>
public static class OutboxMessageRetry
{
    public static void MarkFailed(OutboxMessage message, Exception exception)
    {
        message.Status = OutboxMessageStatus.Failed;
        message.RetryCount++;
        message.LastError = exception.Message;
        message.PublishedAt = null;
    }
}
