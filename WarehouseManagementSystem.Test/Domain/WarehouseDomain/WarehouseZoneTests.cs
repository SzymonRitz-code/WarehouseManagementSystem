using FluentAssertions;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Tests.Support;

namespace WarehouseManagementSystem.Tests.Domain.WarehouseDomain;

/// <summary>
/// Tests for the <see cref="WarehouseZone"/> class in the Warehouse domain, focusing on its properties, methods, and behaviors.
/// </summary>
/// <param name="fixture">The test fixture providing user for the tests.</param>
[Trait("Category", "Warehouse_Zone")]
public class WarehouseZoneTests(DomainTestFixture fixture) : IClassFixture<DomainTestFixture>
{
    /// <summary>
    /// Tests that the constructor of the <see cref="WarehouseZone"/> class initializes
    /// </summary>
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

    /// <summary>
    /// Tests that the <see cref="WarehouseZone.SetCode(string)"/> method throws an exception when an invalid code is provided.
    /// </summary>
    /// <param name="code">The invalid code to test.</param>
    [Theory]
    [ClassData(typeof(InvalidRequiredStringTestData))]
    public void SetCode_ShouldThrow_WhenCodeIsInvalid(string? code)
    {
        var zone = CreateZone();
        Action act = () => zone.SetCode(code!);
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be empty*");
    }

    /// <summary>
    /// Tests that the <see cref="WarehouseZone.SetCode(string)"/> method trims whitespace and converts the code to uppercase.
    /// </summary>
    [Fact]
    public void SetCode_ShouldTrimAndUppercaseCode()
    {
        var zone = CreateZone(code: "z01");
        zone.SetCode(" z02 ");
        zone.Code.Should().Be("Z02");
    }

    /// <summary>
    /// Tests that the <see cref="WarehouseZone.SetName(string)"/> method throws an exception when an invalid name is provided.
    /// </summary>
    /// <param name="name">The invalid name to test.</param>
    [Theory]
    [ClassData(typeof(InvalidRequiredStringTestData))]
    public void SetName_ShouldThrow_WhenNameIsInvalid(string? name)
    {
        var zone = CreateZone();
        Action act = () => zone.SetName(name!);
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be empty*");
    }

    /// <summary>
    /// Tests that the <see cref="WarehouseZone.SetName(string)"/> method trims whitespace from the name.
    /// </summary>
    [Fact]
    public void SetName_ShouldTrimName()
    {
        var zone = CreateZone();
        zone.SetName("  New Zone  ");
        zone.Name.Should().Be("New Zone");
    }

    /// <summary>
    /// Tests that the <see cref="WarehouseZone.SetTemperatureType(TemperatureType)"/> method correctly updates the temperature type.
    /// </summary>
    [Fact]
    public void SetPickingZone_ShouldChangeFlag()
    {
        var zone = CreateZone();
        zone.SetPickingZone(false);
        zone.IsPickingZone.Should().BeFalse();

        zone.SetPickingZone(true);
        zone.IsPickingZone.Should().BeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="WarehouseZone.SetTemperatureType(TemperatureType)"/> method correctly updates the temperature type.
    /// </summary>
    [Fact]
    public void ContainsStock_ShouldReturnFalse_WhenNoStocks()
    {
        var zone = CreateZone();
        zone.ContainsStock().Should().BeFalse();
    }

    /// <summary>
    /// Tests that the <see cref="WarehouseZone.ContainsStock()"/> method returns true when the zone has stocks.
    /// </summary>
    [Fact]
    public void ContainsStock_ShouldReturnTrue_WhenHasStocks()
    {
        var zone = CreateZone();
        zone.Stocks.Add(CreateStock(zone));
        zone.ContainsStock().Should().BeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="WarehouseZone.EnsureCanBeRemoved()"/> method throws an exception when the zone contains stocks.
    /// </summary>
    [Fact]
    public void EnsureCanBeRemoved_ShouldThrow_WhenContainsStocks()
    {
        var zone = CreateZone();
        zone.Stocks.Add(CreateStock(zone, 5));

        Action act = () => zone.EnsureCanBeRemoved();
        act.Should().Throw<InvalidOperationException>().WithMessage("*contains stock*");
    }

    /// <summary>
    /// Tests that the <see cref="WarehouseZone.EnsureCanBeRemoved()"/> method does not throw an exception when the zone has no stocks.
    /// </summary>
    [Fact]
    public void EnsureCanBeRemoved_ShouldNotThrow_WhenNoStocks()
    {
        var zone = CreateZone();
        Action act = () => zone.EnsureCanBeRemoved();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Creates a new instance of <see cref="WarehouseZone"/> with the specified parameters for testing purposes.
    /// </summary>
    /// <param name="code">The code of the warehouse zone.</param>
    /// <param name="name">The name of the warehouse zone.</param>
    /// <param name="temperatureType">The temperature type of the warehouse zone.</param>
    /// <param name="isPickingZone">Indicates whether the warehouse zone is a picking zone.</param>
    /// <param name="warehouseId">The ID of the warehouse.</param>
    /// <returns>A new instance of <see cref="WarehouseZone"/>.</returns>
    private WarehouseZone CreateZone(
        string code = "Z01",
        string name = "Zone 1",
        TemperatureType temperatureType = TemperatureType.Ambient,
        bool isPickingZone = true,
        Guid? warehouseId = null)
    {
        return new(code, name, temperatureType, isPickingZone, warehouseId ?? Guid.NewGuid(), fixture.User);
    }

    /// <summary>
    /// Creates a new instance of <see cref="Stock"/> associated with the specified <see cref="WarehouseZone"/> for testing purposes.
    /// </summary>
    /// <param name="zone">The warehouse zone to associate with the stock.</param>
    /// <param name="quantity">The quantity of the stock.</param>
    /// <returns>A new instance of <see cref="Stock"/>.</returns>
    private static Stock CreateStock(WarehouseZone zone, decimal quantity = 10m)
    {
        return new(Guid.NewGuid(), zone.WarehouseId, zone.Id, null, quantity);
    }
}
