using WarehouseManagementSystem.Infrastructure.Integration;

namespace WarehouseManagementSystem.API.Integration;

/// <summary>Centralizes the state transition used after an unsuccessful broker publication.</summary>
public static class OutboxMessageRetry
{
    /// <summary>Schedules a bounded exponential retry with jitter or abandons a message after its attempt limit.</summary>
    public static void MarkFailed(OutboxMessage message, Exception exception, int maxAttempts = 3, int retryDelaySeconds = 30)
    {
        message.RetryCount++;
        message.LastError = exception.Message;
        message.PublishedAt = null;
        message.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(Math.Min(3600, Math.Max(1, retryDelaySeconds) * Math.Pow(2, Math.Min(message.RetryCount - 1, 16))) * (0.8 + Random.Shared.NextDouble() * 0.4));
        message.Status = message.RetryCount >= maxAttempts ? OutboxMessageStatus.Abandoned : OutboxMessageStatus.Failed;
        if (message.Status == OutboxMessageStatus.Abandoned)
        {
            message.NextAttemptAt = null;
        }
    }
}
