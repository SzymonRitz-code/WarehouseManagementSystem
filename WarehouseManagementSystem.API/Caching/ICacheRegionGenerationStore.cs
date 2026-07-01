namespace WarehouseManagementSystem.API.Caching;

/// <summary>
/// Defines a store for managing generation numbers of cache regions in a distributed caching system.
/// </summary>
public interface ICacheRegionGenerationStore
{
    /// <summary>
    /// Gets the current generation number for the specified cache region asynchronously.
    /// </summary>
    /// <param name="region">The cache region for which to get the generation number.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, with the current generation number as the result.</returns>
    Task<long> GetGenerationAsync(string region, CancellationToken ct = default);

    /// <summary>
    /// Increments the generation number for the specified cache region asynchronously.
    /// </summary>
    /// <param name="region">The cache region for which to increment the generation number.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, with the new generation number as the result.</returns>
    Task<long> IncrementGenerationAsync(string region, CancellationToken ct = default);
}
