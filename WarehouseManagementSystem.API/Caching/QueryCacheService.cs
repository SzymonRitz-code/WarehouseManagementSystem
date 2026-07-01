using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace WarehouseManagementSystem.API.Caching;

/// <summary>
/// Implements a caching service that retrieves or creates cached values for queries, 
/// using a distributed cache (e.g., Redis) and a region-based generation store to manage cache invalidation.
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

    public async Task<T?> GetOrCreateAsync<T>(
        string region,
        string contractVersion,
        IReadOnlyDictionary<string, string> parameters,
        Func<CancellationToken, Task<T?>> factory, //To jest funkcja, ktÃ³ra przyjmuje CancellationToken i zwraca Task<T?>. W kontekÅ›cie tego kodu, factory jest uÅ¼ywane do generowania wartoÅ›ci, jeÅ›li nie zostanie znaleziona w pamiÄ™ci podrÄ™cznej (cache). JeÅ›li wartoÅ›Ä‡ nie istnieje w cache, metoda GetOrCreateAsync wywoÅ‚uje factory, aby uzyskaÄ‡ wartoÅ›Ä‡ z innego ÅºrÃ³dÅ‚a (np. bazy danych) i nastÄ™pnie zapisuje jÄ… w cache.
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
    /// Tries to read a value from the cache for the given key. 
    /// If the value is found, it returns a tuple indicating success and the deserialized value. 
    /// If not found or an error occurs, it returns a tuple indicating failure and a default value.
    /// </summary>
    /// <typeparam name="T">The type of the value to read from the cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A tuple indicating whether the value was found and the deserialized value.</returns>
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
    /// Tries to write a value to the cache for the given key with a specified time-to-live (TTL).
    /// </summary>
    /// <typeparam name="T">The type of the value to write to the cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to write to the cache.</param>
    /// <param name="ttl">The time-to-live (TTL) for the cache entry.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
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
    /// Builds a time-to-live (TTL) value for cache entries with optional jitter to prevent cache stampedes.
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
