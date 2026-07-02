using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WarehouseManagementSystem.API.Caching;

/// <summary>
/// Performs a round-trip probe against the distributed cache backend to validate Redis availability.
/// </summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;

    public RedisHealthCheck(IDistributedCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Writes a short-lived probe value and reads it back to confirm the backend responds correctly.
    /// </summary>
    /// <param name="context">The context in which the health check is being performed.</param>
    /// <param name="cancellationToken">A token to cancel the health check operation.</param>
    /// <returns>A <see cref="HealthCheckResult"/> indicating the health of the Redis cache.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var probeKey = "wms:health:redis";

        try
        {
            await _cache.SetStringAsync(probeKey, "ok", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
            }, cancellationToken);

            var value = await _cache.GetStringAsync(probeKey, cancellationToken);
            return value == "ok"
                ? HealthCheckResult.Healthy("Redis is available.")
                : HealthCheckResult.Degraded("Redis responded with unexpected value.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis unavailable.", ex);
        }
    }
}
