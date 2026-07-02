using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace WarehouseManagementSystem.API.Caching;

/// <summary>
/// Implements read-through query caching with region generations, stampede protection and SQL fallback.
/// </summary>
public sealed class QueryCacheService : IQueryCacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> KeyLocks = new();

    private readonly IDistributedCache _cache;
    private readonly ICacheRegionGenerationStore _generationStore;
    private readonly ICacheKeyBuilder _keyBuilder;
    private readonly RedisCacheOptions _options;
    private readonly ILogger<QueryCacheService> _logger;

    public QueryCacheService(
        IDistributedCache cache,
        ICacheRegionGenerationStore generationStore,
        ICacheKeyBuilder keyBuilder,
        IOptions<RedisCacheOptions> options,
        ILogger<QueryCacheService> logger)
    {
        _cache = cache;
        _generationStore = generationStore;
        _keyBuilder = keyBuilder;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Returns a cached value when present; otherwise resolves it through the factory, stores it and returns it.
    /// </summary>
    /// <typeparam name="T">Type of the cached value.</typeparam>
    /// <param name="region">Logical cache region.</param>
    /// <param name="contractVersion">Cached query contract version.</param>
    /// <param name="parameters">Canonical parameters used to identify the query.</param>
    /// <param name="factory">Fallback factory that loads the value from the underlying data source.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The cached or freshly loaded value.</returns>
    public async Task<T?> GetOrCreateAsync<T>(
        string region,
        string contractVersion,
        IReadOnlyDictionary<string, string> parameters,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return await factory(ct);
        }

        var generation = await _generationStore.GetGenerationAsync(region, ct);
        var key = _keyBuilder.Build(_options.InstancePrefix, region, contractVersion, generation, parameters);

        var redisReadWatch = Stopwatch.StartNew();
        var cached = await TryReadAsync<T>(key, ct); 
        redisReadWatch.Stop();

        if (cached.Found)
        {
            _logger.LogDebug("Cache hit for region {Region} in {ElapsedMs}ms", region, redisReadWatch.ElapsedMilliseconds);
            return cached.Value;
        }

        _logger.LogDebug("Cache miss for region {Region}", region);

        var gate = KeyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        var lockTimeout = TimeSpan.FromMilliseconds(Math.Max(500, _options.OperationTimeoutMs));
        var lockAcquired = await gate.WaitAsync(lockTimeout, ct);

        if (!lockAcquired)
        {
            _logger.LogWarning("Cache stampede lock timeout for region {Region}", region);
            return await factory(ct);
        }

        try
        {
            var secondRead = await TryReadAsync<T>(key, ct);
            if (secondRead.Found)
            {
                _logger.LogDebug("Cache hit after lock wait for region {Region}", region);
                return secondRead.Value;
            }

            var sqlWatch = Stopwatch.StartNew();
            var result = await factory(ct);
            sqlWatch.Stop();

            if (ct.IsCancellationRequested)
            {
                return result;
            }

            if (result is null)
            {
                await TryWriteAsync(key, result, TimeSpan.FromSeconds(_options.NegativeTtlSeconds), ct);
                return result;
            }

            var ttl = BuildJitteredTtl();
            await TryWriteAsync(key, result, ttl, ct);
            _logger.LogDebug("Cache write for region {Region}; SQL fallback took {ElapsedMs}ms", region, sqlWatch.ElapsedMilliseconds);

            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Reads a cached JSON payload and deserializes it into the requested type.
    /// </summary>
    /// <typeparam name="T">Type of the cached value.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple indicating whether a cached value was found and its deserialized value.</returns>
    private async Task<(bool Found, T? Value)> TryReadAsync<T>(string key, CancellationToken ct)
    {
        try
        {
            var json = await _cache.GetStringAsync(key, ct);
            if (string.IsNullOrWhiteSpace(json))
            {
                return (false, default);
            }

            var value = JsonSerializer.Deserialize<T>(json, JsonOptions);
            return (true, value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis read failed. Falling back to SQL.");
            return (false, default);
        }
    }

    /// <summary>
    /// Serializes a value to JSON and stores it in the distributed cache with the provided TTL.
    /// </summary>
    /// <typeparam name="T">Type of the cached value.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Value to store.</param>
    /// <param name="ttl">Time to live for the entry.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task TryWriteAsync<T>(string key, T? value, TimeSpan ttl, CancellationToken ct)
    {
        try
        {
            var payload = JsonSerializer.Serialize(value, JsonOptions);
            await _cache.SetStringAsync(
                key,
                payload,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis write failed for key {CacheKey}", key);
        }
    }

    /// <summary>
    /// Builds a bounded TTL with jitter so many entries do not expire at the same moment.
    /// </summary>
    /// <returns>The calculated TTL value.</returns>
    private TimeSpan BuildJitteredTtl()
    {
        var baseTtl = TimeSpan.FromMinutes(Math.Clamp(_options.DefaultTtlMinutes, 1, 15));
        var jitter = _options.TtlJitterSeconds <= 0
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(Random.Shared.Next(0, _options.TtlJitterSeconds + 1));

        var ttl = baseTtl + jitter;
        var maxTtl = TimeSpan.FromMinutes(15);

        return ttl <= maxTtl ? ttl : maxTtl;
    }
}
