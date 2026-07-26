namespace WarehouseManagementSystem.FakeShipping;

public sealed class ShippingConsumerRetryPolicy
{
    public const string RetryCountHeader = "x-wms-retry-count";
    public const string LastErrorHeader = "x-wms-last-error";
    public const string LastAttemptAtHeader = "x-wms-last-attempt-at";
    private readonly int _maxRetryAttempts;

    public ShippingConsumerRetryPolicy(int maxRetryAttempts) => _maxRetryAttempts = Math.Max(0, maxRetryAttempts);
    public bool ShouldRetry(int completedRetries) => completedRetries < _maxRetryAttempts;
    public int NextRetryCount(int completedRetries) => completedRetries + 1;
    public static int GetRetryCount(IDictionary<string, object>? headers) =>
        headers is not null && headers.TryGetValue(RetryCountHeader, out var value) && value is not null
            ? Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture) : 0;
}
