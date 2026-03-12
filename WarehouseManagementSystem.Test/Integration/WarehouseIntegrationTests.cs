using System;
using FluentAssertions;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using Xunit;

namespace WarehouseManagementSystem.Tests.Integration.WarehouseDomain;

public class WarehouseIntegrationTests
{
    private readonly Guid _productId = Guid.NewGuid();

    private Warehouse CreateWarehouse()
    {
        return new Warehouse(
            "WH01",
            "Main Warehouse",
            "Poland",
            "Warsaw",
            "ul. Example 1");
    }

    [Fact]
    public void Constructor_ShouldInitializeWarehouseCorrectly()
    {
        var warehouse = CreateWarehouse();

        warehouse.Id.Should().NotBeEmpty();
        warehouse.Code.Should().Be("WH01");
        warehouse.Name.Should().Be("Main Warehouse");
        warehouse.Country.Should().Be("Poland");
        warehouse.City.Should().Be("Warsaw");
        warehouse.Address.Should().Be("ul. Example 1");
        warehouse.IsActive.Should().BeTrue();
        warehouse.Zones.Should().BeEmpty();
    }

    [Fact]
    public void AddZone_ShouldAddAndRetrieveZoneCorrectly()
    {
        var warehouse = CreateWarehouse();
        var zone = warehouse.AddZone("Z01", "Zone 1", TemperatureType.Ambient, true);

        zone.Should().NotBeNull();
        zone.Code.Should().Be("Z01");
        warehouse.Zones.Should().Contain(zone);

        var retrieved = warehouse.GetZone(zone.Id);
        retrieved.Should().Be(zone);
    }

    [Fact]
    public void AddZone_ShouldThrow_WhenDuplicateCode()
    {
        var warehouse = CreateWarehouse();
        warehouse.AddZone("Z01", "Zone 1", TemperatureType.Ambient, true);

        Action act = () => warehouse.AddZone("Z01", "Zone 2", TemperatureType.Cold, false);
        act.Should().Throw<InvalidOperationException>().WithMessage("*already exists*");
    }

    [Fact]
    public void RemoveZone_ShouldRemoveZoneCorrectly()
    {
        var warehouse = CreateWarehouse();
        var zone = warehouse.AddZone("Z01", "Zone 1", TemperatureType.Ambient, true);

        warehouse.RemoveZone(zone.Id);
        warehouse.Zones.Should().BeEmpty();
    }

    [Fact]
    public void RemoveZone_ShouldThrow_WhenContainsStock()
    {
        var warehouse = CreateWarehouse();
        var zone = warehouse.AddZone("Z01", "Zone 1", TemperatureType.Ambient, true);

        zone.Stocks.Add(new Stock(_productId, warehouse.Id, zone.Id, null, 10));

        Action act = () => warehouse.RemoveZone(zone.Id);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*containing stock*");
    }

    [Fact]
    public void Deactivate_ShouldThrow_WhenZonesExist()
    {
        var warehouse = CreateWarehouse();
        warehouse.AddZone("Z01", "Zone 1", TemperatureType.Ambient, true);

        Action act = () => warehouse.Deactivate();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*active zones*");
    }

    [Fact]
    public void Deactivate_ShouldThrow_WhenStocksExist()
    {
        var warehouse = CreateWarehouse();
        warehouse.Stocks = new System.Collections.Generic.List<Stock>
        {
            new Stock(_productId, warehouse.Id, Guid.NewGuid(), null, 10)
        };

        Action act = () => warehouse.Deactivate();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*containing stock*");
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        var warehouse = CreateWarehouse();
        warehouse.Deactivate(); // make inactive first
        warehouse.IsActive.Should().BeFalse();

        warehouse.Activate();
        warehouse.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateWarehouseProperties_ShouldWorkCorrectly()
    {
        var warehouse = CreateWarehouse();

        warehouse.SetCode("WH02");
        warehouse.SetName("Secondary Warehouse");
        warehouse.SetLocation("Germany", "Berlin", "Street 123");

        warehouse.Code.Should().Be("WH02");
        warehouse.Name.Should().Be("Secondary Warehouse");
        warehouse.Country.Should().Be("Germany");
        warehouse.City.Should().Be("Berlin");
        warehouse.Address.Should().Be("Street 123");
    }

    [Fact]
    public void GetZone_ShouldThrow_WhenNotFound()
    {
        var warehouse = CreateWarehouse();

        Action act = () => warehouse.GetZone(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }
}