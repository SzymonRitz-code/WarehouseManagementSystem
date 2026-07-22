using WarehouseManagementSystem.Infrastructure.Integration;

namespace WarehouseManagementSystem.API.Integration;

/// <summary>Centralizes the state transition used after an unsuccessful broker publication.</summary>
public static class OutboxMessageRetry
{
    public static void MarkFailed(OutboxMessage message, Exception exception, int maxAttempts = 3, int retryDelaySeconds = 30)
    {
        message.RetryCount++;
        message.LastError = exception.Message;
        message.PublishedAt = null;
        message.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(retryDelaySeconds);
        message.Status = message.RetryCount >= maxAttempts ? OutboxMessageStatus.Abandoned : OutboxMessageStatus.Failed;
        if (message.Status == OutboxMessageStatus.Abandoned)
            message.NextAttemptAt = null;
    }
}
