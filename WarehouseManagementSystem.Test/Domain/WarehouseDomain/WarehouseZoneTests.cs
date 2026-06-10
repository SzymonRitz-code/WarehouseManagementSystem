using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Tests.Domain.WarehouseDomain;

public class WarehouseZoneTests
{
    private readonly Mock<IUserService> _userServiceMock = new Mock<IUserService>();

    public WarehouseZoneTests()
    {
        _userServiceMock.Setup(s => s.GetUser(It.IsAny<HttpContext>()))
            .Returns(new UserSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Testomir.Testowski@gmail.com", "Testomir"));
    }

    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        var warehouseId = Guid.NewGuid();
        var zone = new WarehouseZone(
            "Z01",
            "Zone 1",
            TemperatureType.Ambient,
            true,
            warehouseId,
            _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));

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
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void SetCode_ShouldThrow_WhenCodeIsInvalid(string code)
    {
        var zone = new WarehouseZone("Z01", "Zone 1", TemperatureType.Ambient, true, Guid.NewGuid(), _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        Action act = () => zone.SetCode(code);
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be empty*");
    }

    [Fact]
    public void SetCode_ShouldTrimAndUppercaseCode()
    {
        var zone = new WarehouseZone("z01", "Zone 1", TemperatureType.Ambient, true, Guid.NewGuid(), _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        zone.SetCode(" z02 ");
        zone.Code.Should().Be("Z02");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void SetName_ShouldThrow_WhenNameIsInvalid(string name)
    {
        var zone = new WarehouseZone("Z01", "Zone 1", TemperatureType.Ambient, true, Guid.NewGuid(), _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        Action act = () => zone.SetName(name);
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be empty*");
    }

    [Fact]
    public void SetName_ShouldTrimName()
    {
        var zone = new WarehouseZone("Z01", "Zone 1", TemperatureType.Ambient, true, Guid.NewGuid(), _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        zone.SetName("  New Zone  ");
        zone.Name.Should().Be("New Zone");
    }

    [Fact]
    public void SetPickingZone_ShouldChangeFlag()
    {
        var zone = new WarehouseZone("Z01", "Zone 1", TemperatureType.Ambient, true, Guid.NewGuid(), _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        zone.SetPickingZone(false);
        zone.IsPickingZone.Should().BeFalse();

        zone.SetPickingZone(true);
        zone.IsPickingZone.Should().BeTrue();
    }

    [Fact]
    public void ContainsStock_ShouldReturnFalse_WhenNoStocks()
    {
        var zone = new WarehouseZone("Z01", "Zone 1", TemperatureType.Ambient, true, Guid.NewGuid(), _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        zone.ContainsStock().Should().BeFalse();
    }

    [Fact]
    public void ContainsStock_ShouldReturnTrue_WhenHasStocks()
    {
        var zone = new WarehouseZone("Z01", "Zone 1", TemperatureType.Ambient, true, Guid.NewGuid(), _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        zone.Stocks.Add(new Stock(Guid.NewGuid(), zone.WarehouseId, zone.Id, null, 10));
        zone.ContainsStock().Should().BeTrue();
    }

    [Fact]
    public void EnsureCanBeRemoved_ShouldThrow_WhenContainsStocks()
    {
        var zone = new WarehouseZone("Z01", "Zone 1", TemperatureType.Ambient, true, Guid.NewGuid(), _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        zone.Stocks.Add(new Stock(Guid.NewGuid(), zone.WarehouseId, zone.Id, null, 5));

        Action act = () => zone.EnsureCanBeRemoved();
        act.Should().Throw<InvalidOperationException>().WithMessage("*contains stock*");
    }

    [Fact]
    public void EnsureCanBeRemoved_ShouldNotThrow_WhenNoStocks()
    {
        var zone = new WarehouseZone("Z01", "Zone 1", TemperatureType.Ambient, true, Guid.NewGuid(), _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        Action act = () => zone.EnsureCanBeRemoved();
        act.Should().NotThrow();
    }
}