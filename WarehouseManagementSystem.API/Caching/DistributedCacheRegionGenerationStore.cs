using System.Globalization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace WarehouseManagementSystem.API.Caching;

/// <summary>
/// Stores per-region generation counters in the distributed cache.
/// Incrementing a generation logically invalidates all cached entries built from older generations.
/// </summary>
public sealed class DistributedCacheRegionGenerationStore : ICacheRegionGenerationStore
{
    private static readonly TimeSpan GenerationTtl = TimeSpan.FromDays(365);


    private readonly IDistributedCache _cache;
    private readonly RedisCacheOptions _options;
    private readonly ILogger<DistributedCacheRegionGenerationStore> _logger;

    public DistributedCacheRegionGenerationStore(
        IDistributedCache cache,
        IOptions<RedisCacheOptions> options,
        ILogger<DistributedCacheRegionGenerationStore> logger)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Reads the current generation for a region and initializes it to zero when the counter is missing or invalid.
    /// </summary>
    /// <param name="region">The cache region.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The current region generation.</returns>
    public async Task<long> GetGenerationAsync(string region, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return 0;
        }

        var key = BuildRegionGenerationKey(region);

        try
        {
            var value = await _cache.GetStringAsync(key, ct);
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var generation))
            {
                return generation;
            }

            await _cache.SetStringAsync(
                key,
                "0",
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = GenerationTtl },
                ct);

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read region generation for {Region}", region);
            return 0;
        }
    }

    /// <summary>
    /// Increments the region generation so subsequent reads build keys against a fresh logical version.
    /// </summary>
    /// <param name="region">The cache region.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The next region generation.</returns>
    public async Task<long> IncrementGenerationAsync(string region, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return 0;
        }

        var key = BuildRegionGenerationKey(region);

        try
        {
            var current = await GetGenerationAsync(region, ct);
            var next = current + 1;
            await _cache.SetStringAsync(
                key,
                next.ToString(CultureInfo.InvariantCulture),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = GenerationTtl },
                ct);

            return next;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not increment region generation for {Region}", region);
            return 0;
        }
    }

    /// <summary>
    /// Builds the distributed-cache key used to persist the region generation counter.
    /// </summary>
    /// <param name="region">The cache region.</param>
    /// <returns>The generation storage key.</returns>
    private string BuildRegionGenerationKey(string region)
    {
        return $"{_options.InstancePrefix}:region:{region}:generation";
    }
}
