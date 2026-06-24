using FluentAssertions;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Tests.Support;

namespace WarehouseManagementSystem.Tests.Domain.WarehouseDomain;

 [Trait("Category", "Warehouse_Zone")]
public class WarehouseZoneTests(DomainTestFixture fixture) : IClassFixture<DomainTestFixture>
{
    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        var warehouseId = Guid.NewGuid();
        var zone = CreateZone(warehouseId: warehouseId);

        zone.Id.Should().NotBeEmpty();
        zone.Code.Should().Be("Z01");
        zone.Name.Should().Be("Zone 1");
        zone.TemperatureType.Should().Be(TemperatureType.Ambient);
        zone.IsPickingZone.Should().BeTrue();
        zone.WarehouseId.Should().Be(warehouseId);
        zone.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        zone.Stocks.Should().BeEmpty();
    }

    [Theory]
    [ClassData(typeof(InvalidRequiredStringTestData))]
    public void SetCode_ShouldThrow_WhenCodeIsInvalid(string? code)
    {
        var zone = CreateZone();
        Action act = () => zone.SetCode(code!);
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be empty*");
    }

    [Fact]
    public void SetCode_ShouldTrimAndUppercaseCode()
    {
        var zone = CreateZone(code: "z01");
        zone.SetCode(" z02 ");
        zone.Code.Should().Be("Z02");
    }

    [Theory]
    [ClassData(typeof(InvalidRequiredStringTestData))]
    public void SetName_ShouldThrow_WhenNameIsInvalid(string? name)
    {
        var zone = CreateZone();
        Action act = () => zone.SetName(name!);
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be empty*");
    }

    [Fact]
    public void SetName_ShouldTrimName()
    {
        var zone = CreateZone();
        zone.SetName("  New Zone  ");
        zone.Name.Should().Be("New Zone");
    }

    [Fact]
    public void SetPickingZone_ShouldChangeFlag()
    {
        var zone = CreateZone();
        zone.SetPickingZone(false);
        zone.IsPickingZone.Should().BeFalse();

        zone.SetPickingZone(true);
        zone.IsPickingZone.Should().BeTrue();
    }

    [Fact]
    public void ContainsStock_ShouldReturnFalse_WhenNoStocks()
    {
        var zone = CreateZone();
        zone.ContainsStock().Should().BeFalse();
    }

    [Fact]
    public void ContainsStock_ShouldReturnTrue_WhenHasStocks()
    {
        var zone = CreateZone();
        zone.Stocks.Add(CreateStock(zone));
        zone.ContainsStock().Should().BeTrue();
    }

    [Fact]
    public void EnsureCanBeRemoved_ShouldThrow_WhenContainsStocks()
    {
        var zone = CreateZone();
        zone.Stocks.Add(CreateStock(zone, 5));

        Action act = () => zone.EnsureCanBeRemoved();
        act.Should().Throw<InvalidOperationException>().WithMessage("*contains stock*");
    }

    [Fact]
    public void EnsureCanBeRemoved_ShouldNotThrow_WhenNoStocks()
    {
        var zone = CreateZone();
        Action act = () => zone.EnsureCanBeRemoved();
        act.Should().NotThrow();
    }

    private WarehouseZone CreateZone(
        string code = "Z01",
        string name = "Zone 1",
        TemperatureType temperatureType = TemperatureType.Ambient,
        bool isPickingZone = true,
        Guid? warehouseId = null)
        => new(code, name, temperatureType, isPickingZone, warehouseId ?? Guid.NewGuid(), fixture.User);

    private static Stock CreateStock(WarehouseZone zone, decimal quantity = 10m)
        => new(Guid.NewGuid(), zone.WarehouseId, zone.Id, null, quantity);
}
