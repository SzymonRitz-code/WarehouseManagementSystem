using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Tests.Domain.WarehouseDomain;

public class WarehouseTests
{
    private readonly Mock<IUserService> _userServiceMock = new Mock<IUserService>();

    public WarehouseTests()
    {
        _userServiceMock.Setup(s => s.GetUser(It.IsAny<HttpContext>()))
            .Returns(new UserSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Testomir.Testowski@gmail.com", "Testomir"));
    }

    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        var warehouse = new Warehouse(
            "WH01",
            "Main Warehouse",
            "Poland",
            "Warsaw",
            "ul. Przykładowa 1", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));

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
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void SetCode_ShouldThrowException_WhenInvalidCode(string code)
    {
        var warehouse = new Warehouse("WH01", "Name", "PL", "City", "Address", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        Action act = () => warehouse.SetCode(code);
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void SetName_ShouldThrowException_WhenInvalidName(string name)
    {
        var warehouse = new Warehouse("WH01", "Name", "PL", "City", "Address", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        Action act = () => warehouse.SetName(name);
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be empty*");
    }

    [Theory]
    [InlineData(null, "City", "Address")]
    [InlineData("PL", null, "Address")]
    [InlineData("PL", "City", null)]
    public void SetLocation_ShouldThrowException_WhenInvalidLocation(string country, string city, string address)
    {
        var warehouse = new Warehouse("WH01", "Name", "PL", "City", "Address", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        Action act = () => warehouse.SetLocation(country, city, address);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        var warehouse = new Warehouse("WH01", "Name", "PL", "City", "Address", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        warehouse.Deactivate(); // make inactive first
        warehouse.Activate();
        warehouse.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldThrow_WhenZonesExist()
    {
        var warehouse = new Warehouse("WH01", "Name", "PL", "City", "Address", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        Action act = () => warehouse.Deactivate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*active zones*");
    }

    [Fact]
    public void Deactivate_ShouldThrow_WhenStocksExist()
    {
        var warehouse = new Warehouse("WH01", "Name", "PL", "City", "Address", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        warehouse.Stocks = new System.Collections.Generic.List<Stock>
        {
            new Stock(Guid.NewGuid(), warehouse.Id, Guid.NewGuid(), null, 10)
        };

        Action act = () => warehouse.Deactivate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*containing stock*");
    }

    [Fact]
    public void AddZone_ShouldAddZoneCorrectly()
    {
        var warehouse = new Warehouse("WH01", "Name", "PL", "City", "Address", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        var zone = warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        zone.Should().NotBeNull();
        zone.Code.Should().Be("Z1");
        warehouse.Zones.Should().Contain(zone);
    }

    [Fact]
    public void AddZone_ShouldThrow_WhenDuplicateCode()
    {
        var warehouse = new Warehouse("WH01", "Name", "PL", "City", "Address", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        Action act = () => warehouse.AddZone("Z1", "Zone 2", TemperatureType.Cold, false);
        act.Should().Throw<InvalidOperationException>().WithMessage("*already exists*");
    }

    [Fact]
    public void RemoveZone_ShouldRemoveZoneCorrectly()
    {
        var warehouse = new Warehouse("WH01", "Name", "PL", "City", "Address", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        var zone = warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        warehouse.RemoveZone(zone.Id);
        warehouse.Zones.Should().BeEmpty();
    }

    [Fact]
    public void RemoveZone_ShouldThrow_WhenZoneNotFound()
    {
        var warehouse = new Warehouse("WH01", "Name", "PL", "City", "Address", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));

        Action act = () => warehouse.RemoveZone(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public void RemoveZone_ShouldThrow_WhenZoneContainsStock()
    {
        var warehouse = new Warehouse("WH01", "Name", "PL", "City", "Address", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        var zone = warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        zone.Stocks.Add(new Stock(Guid.NewGuid(), warehouse.Id, zone.Id, null, 10));

        Action act = () => warehouse.RemoveZone(zone.Id);
        act.Should().Throw<InvalidOperationException>().WithMessage("*containing stock*");
    }

    [Fact]
    public void GetZone_ShouldReturnCorrectZone()
    {
        var warehouse = new Warehouse("WH01", "Name", "PL", "City", "Address", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        var zone = warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        var result = warehouse.GetZone(zone.Id);
        result.Should().Be(zone);
    }

    [Fact]
    public void GetZone_ShouldThrow_WhenNotFound()
    {
        var warehouse = new Warehouse("WH01", "Name", "PL", "City", "Address", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        Action act = () => warehouse.GetZone(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }
}