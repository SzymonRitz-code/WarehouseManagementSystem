using FluentAssertions;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Tests.Support;

namespace WarehouseManagementSystem.Tests.Domain.WarehouseDomain;

[Trait("Category", "Warehouse_Aggregate")]
public class WarehouseAggregateFlowTests(DomainTestFixture fixture) : IClassFixture<DomainTestFixture>
{
    private readonly Guid _productId = Guid.NewGuid();

    [Fact]
    public void Constructor_ShouldInitializeWarehouseCorrectly()
    {
        // Arrange
        var warehouse = CreateWarehouse();

        // Act
        var zones = warehouse.Zones;

        // Assert
        warehouse.Id.Should().NotBeEmpty();
        warehouse.Code.Should().Be("WH01");
        warehouse.Name.Should().Be("Main Warehouse");
        warehouse.Country.Should().Be("Poland");
        warehouse.City.Should().Be("Warsaw");
        warehouse.Address.Should().Be("ul. Example 1");
        warehouse.IsActive.Should().BeTrue();
        zones.Should().BeEmpty();
    }

    [Fact]
    public void AddZone_ShouldAddAndRetrieveZoneCorrectly()
    {
        // Arrange
        var warehouse = CreateWarehouse();

        // Act
        var zone = warehouse.AddZone("Z01", "Zone 1", TemperatureType.Ambient, true);
        var retrieved = warehouse.GetZone(zone.Id);

        // Assert
        zone.Should().NotBeNull();
        zone.Code.Should().Be("Z01");
        warehouse.Zones.Should().Contain(zone);
        retrieved.Should().Be(zone);
    }

    [Fact]
    public void AddZone_ShouldThrow_WhenDuplicateCode()
    {
        // Arrange
        var warehouse = CreateWarehouse();
        warehouse.AddZone("Z01", "Zone 1", TemperatureType.Ambient, true);

        // Act
        Action act = () => warehouse.AddZone("Z01", "Zone 2", TemperatureType.Cold, false);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*already exists*");
    }

    [Fact]
    public void RemoveZone_ShouldRemoveZoneCorrectly()
    {
        // Arrange
        var warehouse = CreateWarehouse();
        var zone = warehouse.AddZone("Z01", "Zone 1", TemperatureType.Ambient, true);

        // Act
        warehouse.RemoveZone(zone.Id);

        // Assert
        warehouse.Zones.Should().BeEmpty();
    }

    [Fact]
    public void RemoveZone_ShouldThrow_WhenContainsStock()
    {
        // Arrange
        var warehouse = CreateWarehouse();
        var zone = warehouse.AddZone("Z01", "Zone 1", TemperatureType.Ambient, true);

        zone.Stocks.Add(CreateStock(warehouse.Id, zone.Id));

        // Act
        Action act = () => warehouse.RemoveZone(zone.Id);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*containing stock*");
    }

    [Fact]
    public void Deactivate_ShouldThrow_WhenZonesExist()
    {
        // Arrange
        var warehouse = CreateWarehouse();
        warehouse.AddZone("Z01", "Zone 1", TemperatureType.Ambient, true);

        // Act
        Action act = () => warehouse.Deactivate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*active zones*");
    }

    [Fact]
    public void Deactivate_ShouldThrow_WhenStocksExist()
    {
        // Arrange
        var warehouse = CreateWarehouse();
        warehouse.Stocks = [CreateStock(warehouse.Id, Guid.NewGuid())];

        // Act
        Action act = () => warehouse.Deactivate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*containing stock*");
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var warehouse = CreateWarehouse();
        warehouse.Deactivate();

        // Act
        warehouse.Activate();

        // Assert
        warehouse.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateWarehouseProperties_ShouldWorkCorrectly()
    {
        // Arrange
        var warehouse = CreateWarehouse();

        // Act
        warehouse.SetCode("WH02");
        warehouse.SetName("Secondary Warehouse");
        warehouse.SetLocation("Germany", "Berlin", "Street 123");

        // Assert
        warehouse.Code.Should().Be("WH02");
        warehouse.Name.Should().Be("Secondary Warehouse");
        warehouse.Country.Should().Be("Germany");
        warehouse.City.Should().Be("Berlin");
        warehouse.Address.Should().Be("Street 123");
    }

    [Fact]
    public void GetZone_ShouldThrow_WhenNotFound()
    {
        // Arrange
        var warehouse = CreateWarehouse();

        // Act
        Action act = () => warehouse.GetZone(Guid.NewGuid());

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }

    private Warehouse CreateWarehouse()
        => new(
            "WH01",
            "Main Warehouse",
            "Poland",
            "Warsaw",
            "ul. Example 1",
            fixture.User);

    private Stock CreateStock(Guid warehouseId, Guid zoneId)
        => new(_productId, warehouseId, zoneId, null, 10);
}
