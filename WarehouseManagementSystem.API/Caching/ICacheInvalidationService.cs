namespace WarehouseManagementSystem.API.Caching;

/// <summary>
/// Defines a service for invalidating cache regions in a distributed caching system.
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    /// Invalidates the specified cache regions asynchronously.
    /// </summary>
    /// <param name="regions">The cache regions to invalidate.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InvalidateRegionsAsync(IEnumerable<string> regions, CancellationToken ct = default);
}
