using FluentAssertions;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Tests.Support;

namespace WarehouseManagementSystem.Tests.Domain.WarehouseDomain;

 [Trait("Category", "Warehouse")]
public class WarehouseTests(DomainTestFixture fixture) : IClassFixture<DomainTestFixture>
{
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

    [Theory]
    [ClassData(typeof(InvalidRequiredStringTestData))]
    public void SetCode_ShouldThrowException_WhenInvalidCode(string? code)
    {
        var warehouse = CreateWarehouse();
        Action act = () => warehouse.SetCode(code!);
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be empty*");
    }

    [Theory]
    [ClassData(typeof(InvalidRequiredStringTestData))]
    public void SetName_ShouldThrowException_WhenInvalidName(string? name)
    {
        var warehouse = CreateWarehouse();
        Action act = () => warehouse.SetName(name!);
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be empty*");
    }

    [Theory]
    [ClassData(typeof(InvalidWarehouseLocationTestData))]
    public void SetLocation_ShouldThrowException_WhenInvalidLocation(string? country, string? city, string? address)
    {
        var warehouse = CreateWarehouse();
        Action act = () => warehouse.SetLocation(country!, city!, address!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        var warehouse = CreateWarehouse();
        warehouse.Deactivate(); // make inactive first
        warehouse.Activate();
        warehouse.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldThrow_WhenZonesExist()
    {
        var warehouse = CreateWarehouse();
        warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        Action act = () => warehouse.Deactivate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*active zones*");
    }

    [Fact]
    public void Deactivate_ShouldThrow_WhenStocksExist()
    {
        var warehouse = CreateWarehouse();
        warehouse.Stocks = [CreateStock(warehouse.Id, Guid.NewGuid())];

        Action act = () => warehouse.Deactivate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*containing stock*");
    }

    [Fact]
    public void AddZone_ShouldAddZoneCorrectly()
    {
        var warehouse = CreateWarehouse();
        var zone = warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        zone.Should().NotBeNull();
        zone.Code.Should().Be("Z1");
        warehouse.Zones.Should().Contain(zone);
    }

    [Fact]
    public void AddZone_ShouldThrow_WhenDuplicateCode()
    {
        var warehouse = CreateWarehouse();
        warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        Action act = () => warehouse.AddZone("Z1", "Zone 2", TemperatureType.Cold, false);
        act.Should().Throw<InvalidOperationException>().WithMessage("*already exists*");
    }

    [Fact]
    public void RemoveZone_ShouldRemoveZoneCorrectly()
    {
        var warehouse = CreateWarehouse();
        var zone = warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        warehouse.RemoveZone(zone.Id);
        warehouse.Zones.Should().BeEmpty();
    }

    [Fact]
    public void RemoveZone_ShouldThrow_WhenZoneNotFound()
    {
        var warehouse = CreateWarehouse();

        Action act = () => warehouse.RemoveZone(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public void RemoveZone_ShouldThrow_WhenZoneContainsStock()
    {
        var warehouse = CreateWarehouse();
        var zone = warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        zone.Stocks.Add(CreateStock(warehouse.Id, zone.Id));

        Action act = () => warehouse.RemoveZone(zone.Id);
        act.Should().Throw<InvalidOperationException>().WithMessage("*containing stock*");
    }

    [Fact]
    public void GetZone_ShouldReturnCorrectZone()
    {
        var warehouse = CreateWarehouse();
        var zone = warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        var result = warehouse.GetZone(zone.Id);
        result.Should().Be(zone);
    }

    [Fact]
    public void GetZone_ShouldThrow_WhenNotFound()
    {
        var warehouse = CreateWarehouse();
        Action act = () => warehouse.GetZone(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }

    private Warehouse CreateWarehouse(
        string code = "WH01",
        string name = "Name",
        string country = "PL",
        string city = "City",
        string address = "Address")
        => new(code, name, country, city, address, fixture.User);

    private static Stock CreateStock(Guid warehouseId, Guid zoneId)
        => new(Guid.NewGuid(), warehouseId, zoneId, null, 10);
}
