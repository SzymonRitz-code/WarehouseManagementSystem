using FluentAssertions;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Tests.Support;

namespace WarehouseManagementSystem.Tests.Domain.WarehouseDomain;

/// <summary>
/// Tests for the <see cref="Warehouse"/> aggregate in the Warehouse domain, focusing on the flow of operations such as adding/removing zones, activating/deactivating the warehouse, and managing stocks.
/// </summary>
/// <param name="fixture">The test fixture providing user for the tests.</param>
[Trait("Category", "Warehouse_Aggregate")]
public class WarehouseAggregateFlowTests(DomainTestFixture fixture) : IClassFixture<DomainTestFixture>
{
    private readonly Guid _productId = Guid.NewGuid();

    /// <summary>
    /// Tests that the constructor of the <see cref="Warehouse"/> class initializes the warehouse correctly with the provided parameters and sets default values for properties like IsActive and Zones.
    /// </summary>
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
    /// <summary>
    /// Tests that adding a zone to the warehouse works correctly, and that the zone can be retrieved by its ID. It also verifies that the zone is added to the warehouse's collection of zones.
    /// </summary>
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
    /// <summary>
    /// Tests that adding a zone with a duplicate code throws an InvalidOperationException, ensuring that zone codes are unique within the warehouse.
    /// </summary>
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
    /// <summary>
    /// Tests that removing a zone from the warehouse works correctly, and that the zone is no longer present in the warehouse's collection of zones after removal.
    /// </summary>
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
    /// <summary>
    /// Tests that attempting to remove a zone that contains stock throws an InvalidOperationException, ensuring that zones with existing stock cannot be removed from the warehouse.
    /// </summary>
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
    /// <summary>
    /// Tests that deactivating the warehouse throws an InvalidOperationException when there are active zones present, 
    /// ensuring that a warehouse cannot be deactivated while it still has zones.
    /// </summary>
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
    /// <summary>
    /// Tests that deactivating the warehouse throws an InvalidOperationException when there are stocks present, 
    /// ensuring that a warehouse cannot be deactivated while it still has stock in any of its zones.
    /// </summary>
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
    /// <summary>
    /// Tests that deactivating the warehouse sets the IsActive property to false, indicating that the warehouse is no longer active.
    /// </summary>
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

    /// <summary>
    /// Tests that updating the warehouse properties (code, name, and location) works correctly, and that the updated values are reflected in the warehouse's properties.
    /// </summary>
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

    /// <summary>
    /// Tests that attempting to retrieve a zone that does not exist in the warehouse throws an InvalidOperationException, 
    /// ensuring that the GetZone method correctly handles cases where the specified zone ID is not found.
    /// </summary>
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

    /// <summary>
    /// Creates a new instance of the <see cref="Warehouse"/> class with predefined properties for testing purposes.
    /// </summary>
    /// <returns>A new instance of the <see cref="Warehouse"/> class.</returns>
    private Warehouse CreateWarehouse()
        => new(
                "WH01",
                "Main Warehouse",
                "Poland",
                "Warsaw",
                "ul. Example 1",
                fixture.User);

    /// <summary>
    /// Creates a new instance of the <see cref="Stock"/> class with predefined properties for testing purposes.
    /// </summary>
    /// <param name="warehouseId">The ID of the warehouse to which the stock belongs.</param>
    /// <param name="zoneId">The ID of the zone to which the stock belongs.</param>
    /// <returns>A new instance of the <see cref="Stock"/> class.</returns>
    private Stock CreateStock(Guid warehouseId, Guid zoneId)
        => new(_productId, warehouseId, zoneId, null, 10);
}
