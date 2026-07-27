using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WarehouseManagementSystem.API.Caching;

namespace WarehouseManagementSystem.Tests.Services.Queries;

public class QueryCacheServiceTests
{
    [Fact]
    public async Task GetOrCreateAsync_ShouldCacheValue_OnMiss()
    {
        var cache = new TestDistributedCache();
        var options = Options.Create(new RedisCacheOptions { Enabled = true, InstancePrefix = "wms", DefaultTtlMinutes = 15, TtlJitterSeconds = 0 });
        var generationStore = new DistributedCacheRegionGenerationStore(cache, options, NullLogger<DistributedCacheRegionGenerationStore>.Instance);
        var service = new QueryCacheService(cache, generationStore, new CacheKeyBuilder(), options, NullLogger<QueryCacheService>.Instance);

        var callCount = 0;
        var parameters = new Dictionary<string, string> { ["page"] = "1" };

        var first = await service.GetOrCreateAsync(
            CacheRegions.Products,
            "v1",
            parameters,
            _ =>
            {
                callCount++;
                return Task.FromResult<PagedResultLike?>(new PagedResultLike(5));
            });

        var second = await service.GetOrCreateAsync(
            CacheRegions.Products,
            "v1",
            parameters,
            _ =>
            {
                callCount++;
                return Task.FromResult<PagedResultLike?>(new PagedResultLike(9));
            });

        first!.Count.Should().Be(5);
        second!.Count.Should().Be(5);
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldUseFactory_WhenCacheDisabled()
    {
        var cache = new TestDistributedCache();
        var options = Options.Create(new RedisCacheOptions { Enabled = false, InstancePrefix = "wms" });
        var generationStore = new DistributedCacheRegionGenerationStore(cache, options, NullLogger<DistributedCacheRegionGenerationStore>.Instance);
        var service = new QueryCacheService(cache, generationStore, new CacheKeyBuilder(), options, NullLogger<QueryCacheService>.Instance);

        var calls = 0;
        var parameters = new Dictionary<string, string> { ["scope"] = "x" };

        await service.GetOrCreateAsync(CacheRegions.Products, "v1", parameters, _ =>
        {
            calls++;
            return Task.FromResult<PagedResultLike?>(new PagedResultLike(1));
        });

        await service.GetOrCreateAsync(CacheRegions.Products, "v1", parameters, _ =>
        {
            calls++;
            return Task.FromResult<PagedResultLike?>(new PagedResultLike(2));
        });

        calls.Should().Be(2);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldCacheNull_WithNegativeTtl()
    {
        var cache = new TestDistributedCache();
        var options = Options.Create(new RedisCacheOptions { Enabled = true, InstancePrefix = "wms", NegativeTtlSeconds = 30, DefaultTtlMinutes = 15, TtlJitterSeconds = 0 });
        var generationStore = new DistributedCacheRegionGenerationStore(cache, options, NullLogger<DistributedCacheRegionGenerationStore>.Instance);
        var service = new QueryCacheService(cache, generationStore, new CacheKeyBuilder(), options, NullLogger<QueryCacheService>.Instance);

        var calls = 0;
        var parameters = new Dictionary<string, string> { ["id"] = "missing" };

        var first = await service.GetOrCreateAsync<object>(CacheRegions.Products, "v1", parameters, _ =>
        {
            calls++;
            return Task.FromResult<object?>(null);
        });

        var second = await service.GetOrCreateAsync<object>(CacheRegions.Products, "v1", parameters, _ =>
        {
            calls++;
            return Task.FromResult<object?>(new { Unexpected = true });
        });

        first.Should().BeNull();
        second.Should().BeNull();
        calls.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldProtectAgainstStampede()
    {
        var cache = new TestDistributedCache();
        var options = Options.Create(new RedisCacheOptions { Enabled = true, InstancePrefix = "wms", DefaultTtlMinutes = 15, TtlJitterSeconds = 0 });
        var generationStore = new DistributedCacheRegionGenerationStore(cache, options, NullLogger<DistributedCacheRegionGenerationStore>.Instance);
        var service = new QueryCacheService(cache, generationStore, new CacheKeyBuilder(), options, NullLogger<QueryCacheService>.Instance);

        var calls = 0;
        var parameters = new Dictionary<string, string> { ["page"] = "1" };

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => service.GetOrCreateAsync(
                CacheRegions.Stocks,
                "v1",
                parameters,
                async _ =>
                {
                    Interlocked.Increment(ref calls);
                    await Task.Delay(50);
                    return new PagedResultLike(7);
                }))
            .ToArray();

        await Task.WhenAll(tasks);

        calls.Should().Be(1);
        tasks.All(t => t.Result?.Count == 7).Should().BeTrue();
    }

    private sealed record PagedResultLike(int Count);

    /// <summary>
    /// Thread-safe in-memory IDistributedCache backed by a ConcurrentDictionary.
    /// Needed so stampede-protection tests observe correct read-after-write behaviour
    /// when multiple tasks race concurrently.
    /// </summary>
    private sealed class TestDistributedCache : IDistributedCache
    {
        private readonly ConcurrentDictionary<string, byte[]> _store = new(StringComparer.Ordinal);

        public byte[]? Get(string key)
        {
            _store.TryGetValue(key, out var value);
            return value;
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
            => Task.FromResult(Get(key));

        public void Refresh(string key) { }

        public Task RefreshAsync(string key, CancellationToken token = default)
            => Task.CompletedTask;

        public void Remove(string key) => _store.TryRemove(key, out _);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
            => _store[key] = value;

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }
}
