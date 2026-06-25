using FluentAssertions;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Tests.Support;

namespace WarehouseManagementSystem.Tests.Domain.WarehouseDomain;

/// <summary>
/// Tests for the <see cref="Warehouse"/> class in the Warehouse domain, focusing on its properties, methods, and behaviors.
/// </summary>
/// <param name="fixture">The test fixture providing user for the tests.</param>
[Trait("Category", "Warehouse")]
public class WarehouseTests(DomainTestFixture fixture) : IClassFixture<DomainTestFixture>
{
    /// <summary>
    /// Tests that the constructor initializes the properties of the <see cref="Warehouse"/> class correctly.
    /// </summary>
    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        var warehouse = CreateWarehouse(
            code: "WH01",
            name: "Main Warehouse",
            country: "Poland",
            city: "Warsaw",
            address: "ul. Przykładowa 1");

        warehouse.Id.Should().NotBeEmpty();
        warehouse.Code.Should().Be("WH01");
        warehouse.Name.Should().Be("Main Warehouse");
        warehouse.Country.Should().Be("Poland");
        warehouse.City.Should().Be("Warsaw");
        warehouse.Address.Should().Be("ul. Przykładowa 1");
        warehouse.IsActive.Should().BeTrue();
        warehouse.Zones.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the SetCode method throws an ArgumentException when an invalid code is provided.
    /// </summary>
    /// <param name="code">The invalid code to test.</param>
    [Theory]
    [ClassData(typeof(InvalidRequiredStringTestData))]
    public void SetCode_ShouldThrowException_WhenInvalidCode(string? code)
    {
        var warehouse = CreateWarehouse();
        Action act = () => warehouse.SetCode(code!);
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be empty*");
    }

    /// <summary>
    /// Tests that the SetName method throws an ArgumentException when an invalid name is provided.
    /// </summary>
    /// <param name="name">The invalid name to test.</param>
    [Theory]
    [ClassData(typeof(InvalidRequiredStringTestData))]
    public void SetName_ShouldThrowException_WhenInvalidName(string? name)
    {
        var warehouse = CreateWarehouse();
        Action act = () => warehouse.SetName(name!);
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be empty*");
    }

    /// <summary>
    /// Tests that the SetLocation method throws an ArgumentException when invalid location parameters are provided.
    /// </summary>
    /// <param name="country">The invalid country to test.</param>
    /// <param name="city">The invalid city to test.</param>
    /// <param name="address">The invalid address to test.</param>
    [Theory]
    [ClassData(typeof(InvalidWarehouseLocationTestData))]
    public void SetLocation_ShouldThrowException_WhenInvalidLocation(string? country, string? city, string? address)
    {
        var warehouse = CreateWarehouse();
        Action act = () => warehouse.SetLocation(country!, city!, address!);
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that the Activate method sets the IsActive property to true.
    /// </summary>
    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        var warehouse = CreateWarehouse();
        warehouse.Deactivate(); // make inactive first
        warehouse.Activate();
        warehouse.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Tests that the Deactivate method throws an InvalidOperationException when there are active zones in the warehouse.
    /// </summary>
    [Fact]
    public void Deactivate_ShouldThrow_WhenZonesExist()
    {
        var warehouse = CreateWarehouse();
        warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        Action act = () => warehouse.Deactivate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*active zones*");
    }

    /// <summary>
    /// Tests that the Deactivate method throws an InvalidOperationException when there are stocks in the warehouse.
    /// </summary>
    [Fact]
    public void Deactivate_ShouldThrow_WhenStocksExist()
    {
        var warehouse = CreateWarehouse();
        warehouse.Stocks = [CreateStock(warehouse.Id, Guid.NewGuid())];

        Action act = () => warehouse.Deactivate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*containing stock*");
    }

    /// <summary>
    /// Tests that the AddZone method adds a zone correctly to the warehouse.
    /// </summary>
    [Fact]
    public void AddZone_ShouldAddZoneCorrectly()
    {
        var warehouse = CreateWarehouse();
        var zone = warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        zone.Should().NotBeNull();
        zone.Code.Should().Be("Z1");
        warehouse.Zones.Should().Contain(zone);
    }

    /// <summary>
    /// Tests that the AddZone method throws an InvalidOperationException when trying to add a zone with a duplicate code.
    /// </summary>
    [Fact]
    public void AddZone_ShouldThrow_WhenDuplicateCode()
    {
        var warehouse = CreateWarehouse();
        warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        Action act = () => warehouse.AddZone("Z1", "Zone 2", TemperatureType.Cold, false);
        act.Should().Throw<InvalidOperationException>().WithMessage("*already exists*");
    }

    /// <summary>
    /// Tests that the RemoveZone method removes a zone correctly from the warehouse.
    /// </summary>
    [Fact]
    public void RemoveZone_ShouldRemoveZoneCorrectly()
    {
        var warehouse = CreateWarehouse();
        var zone = warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        warehouse.RemoveZone(zone.Id);
        warehouse.Zones.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the RemoveZone method throws an InvalidOperationException when trying to remove a zone that does not exist.
    /// </summary>
    [Fact]
    public void RemoveZone_ShouldThrow_WhenZoneNotFound()
    {
        var warehouse = CreateWarehouse();

        Action act = () => warehouse.RemoveZone(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }

    /// <summary>
    /// Tests that the RemoveZone method throws an InvalidOperationException when trying to remove a zone that contains stock.
    /// </summary>
    [Fact]
    public void RemoveZone_ShouldThrow_WhenZoneContainsStock()
    {
        var warehouse = CreateWarehouse();
        var zone = warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        zone.Stocks.Add(CreateStock(warehouse.Id, zone.Id));

        Action act = () => warehouse.RemoveZone(zone.Id);
        act.Should().Throw<InvalidOperationException>().WithMessage("*containing stock*");
    }

    /// <summary>
    /// Tests that the GetZone method returns the correct zone when it exists in the warehouse.
    /// </summary>
    [Fact]
    public void GetZone_ShouldReturnCorrectZone()
    {
        var warehouse = CreateWarehouse();
        var zone = warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        var result = warehouse.GetZone(zone.Id);
        result.Should().Be(zone);
    }

    /// <summary>
    /// Tests that the GetZone method throws an InvalidOperationException when trying to retrieve a zone that does not exist in the warehouse.
    /// </summary>
    [Fact]
    public void GetZone_ShouldThrow_WhenNotFound()
    {
        var warehouse = CreateWarehouse();
        Action act = () => warehouse.GetZone(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }

    /// <summary>
    /// Creates a new instance of the <see cref="Warehouse"/> class with the specified parameters.
    /// </summary>
    /// <param name="code">The code of the warehouse.</param>
    /// <param name="name">The name of the warehouse.</param>
    /// <param name="country">The country where the warehouse is located.</param>
    /// <param name="city">The city where the warehouse is located.</param>
    /// <param name="address">The address of the warehouse.</param>
    /// <returns>A new instance of the <see cref="Warehouse"/> class.</returns>
    private Warehouse CreateWarehouse(
        string code = "WH01",
        string name = "Name",
        string country = "PL",
        string city = "City",
        string address = "Address")
    {
        return new(code, name, country, city, address, fixture.User);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="Stock"/> class with the specified warehouse and zone IDs.
    /// </summary>
    /// <param name="warehouseId">The ID of the warehouse.</param>
    /// <param name="zoneId">The ID of the zone.</param>
    /// <returns>A new instance of the <see cref="Stock"/> class.</returns>
    private static Stock CreateStock(Guid warehouseId, Guid zoneId)
    {
        return new(Guid.NewGuid(), warehouseId, zoneId, null, 10);
    }
}
