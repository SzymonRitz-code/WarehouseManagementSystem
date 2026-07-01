namespace WarehouseManagementSystem.API.Caching;

/// <summary>
/// Defines a service for caching query results in a distributed caching system.
/// </summary>
public interface IQueryCacheService
{
    /// <summary>
    /// Gets a cached value for the specified region, contract version, and parameters. 
    /// If the value is not found in the cache, it invokes the provided factory function 
    /// to create the value, caches it, and then returns it.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="region">The cache region.</param>
    /// <param name="contractVersion">The contract version.</param>
    /// <param name="parameters">The parameters for the cache key.</param>
    /// <param name="factory">The factory function to create the value if it is not found in the cache.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, with the cached value as the result.</returns>
    Task<T?> GetOrCreateAsync<T>(
        string region,
        string contractVersion,
        IReadOnlyDictionary<string, string> parameters,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken ct = default);
}
