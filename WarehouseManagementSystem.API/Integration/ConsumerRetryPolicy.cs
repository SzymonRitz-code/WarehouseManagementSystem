namespace WarehouseManagementSystem.API.Integration;

/// <summary>Decides whether a failed delivery may be retried without creating a hot requeue loop.</summary>
public sealed class ConsumerRetryPolicy
{
    public const string RetryCountHeader = "x-wms-retry-count";
    public const string LastErrorHeader = "x-wms-last-error";
    public const string LastAttemptAtHeader = "x-wms-last-attempt-at";

    private readonly int _maxRetryAttempts;

    public ConsumerRetryPolicy(int maxRetryAttempts)
    {
        _maxRetryAttempts = maxRetryAttempts >= 0 ? maxRetryAttempts : 0;
    }

    public bool ShouldRetry(int completedRetries) => completedRetries < _maxRetryAttempts;

    public int NextRetryCount(int completedRetries) => completedRetries + 1;

    public static int GetRetryCount(IDictionary<string, object>? headers)
    {
        if (headers is null || !headers.TryGetValue(RetryCountHeader, out var value) || value is null)
            return 0;

        return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
    }
}
