using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace WarehouseManagementSystem.API.Caching;

/// <summary>
/// Represents a store for managing cache region generations using a distributed cache.
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
    /// Gets the current generation for the specified cache region.
    /// </summary>
    /// <param name="region">The cache region.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The current generation for the cache region.</returns>
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
    /// Increments the generation for the specified cache region.
    /// </summary>
    /// <param name="region">The cache region.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The new generation for the cache region.</returns>
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
    /// Builds the cache key for the generation of the specified region.
    /// </summary>
    /// <param name="region">The cache region.</param>
    /// <returns>The cache key for the region's generation.</returns>
    private string BuildRegionGenerationKey(string region) => $"{_options.InstancePrefix}:region:{region}:generation";
}
