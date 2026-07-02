namespace WarehouseManagementSystem.API.Caching;

/// <summary>
/// Invalidates cache regions by bumping their generation counters in the shared store.
/// This avoids scanning or deleting individual keys in Redis.
/// </summary>
public sealed class CacheInvalidationService : ICacheInvalidationService
{
    private readonly ICacheRegionGenerationStore _generationStore;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        ICacheRegionGenerationStore generationStore,
        ILogger<CacheInvalidationService> logger)
    {
        _generationStore = generationStore;
        _logger = logger;
    }

    /// <summary>
    /// Invalidates each distinct region once by incrementing its generation counter.
    /// </summary>
    /// <param name="regions">The cache regions to invalidate.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvalidateRegionsAsync(IEnumerable<string> regions, CancellationToken ct = default)
    {
        foreach (var region in regions.Distinct(StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var generation = await _generationStore.IncrementGenerationAsync(region, ct);
                _logger.LogInformation("Cache region {Region} invalidated to generation {Generation}", region, generation);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache invalidation failed for region {Region}", region);
            }
        }
    }
}
