using FluentAssertions;
using WarehouseManagementSystem.API.Caching;

namespace WarehouseManagementSystem.Tests.Services.Queries;

public class CacheKeyBuilderTests
{
    private readonly CacheKeyBuilder _builder = new();

    [Fact]
    public void Build_ShouldReturnSameKey_ForLogicallyEquivalentParameters()
    {
        var first = new Dictionary<string, string>
        {
            ["page"] = "1",
            ["search"] = CacheKeyNormalizer.NormalizeString("  abc  "),
            ["sortBy"] = CacheKeyNormalizer.NormalizeSort("Name")
        };

        var second = new Dictionary<string, string>
        {
            ["sortBy"] = CacheKeyNormalizer.NormalizeSort("name"),
            ["search"] = CacheKeyNormalizer.NormalizeString("abc"),
            ["page"] = "1"
        };

        var keyA = _builder.Build("wms", CacheRegions.Products, "v1", 42, first);
        var keyB = _builder.Build("wms", CacheRegions.Products, "v1", 42, second);

        keyA.Should().Be(keyB);
    }

    [Fact]
    public void Build_ShouldReturnDifferentKeys_ForDifferentPageOrSorting()
    {
        var baseParams = new Dictionary<string, string>
        {
            ["page"] = "1",
            ["pageSize"] = "10",
            ["sortBy"] = "sku",
            ["sortDirection"] = "asc"
        };

        var otherParams = new Dictionary<string, string>
        {
            ["page"] = "2",
            ["pageSize"] = "10",
            ["sortBy"] = "sku",
            ["sortDirection"] = "asc"
        };

        var keyA = _builder.Build("wms", CacheRegions.Products, "v1", 1, baseParams);
        var keyB = _builder.Build("wms", CacheRegions.Products, "v1", 1, otherParams);

        keyA.Should().NotBe(keyB);
    }
}
