using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Tests.Integration.InventoryDomain;

public class ReservationIntegrationTests
{
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _zoneId = Guid.NewGuid();
    private readonly Mock<IUserService> _userServiceMock = new();
    public ReservationIntegrationTests()
    {
        _userServiceMock.Setup(s => s.GetUser(It.IsAny<HttpContext>()))
    .Returns(new UserSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Testomir.Testowski@gmail.com", "Testomir"));
    }

    private Stock CreateStock(decimal initialQuantity = 100m)
    {
        return new Stock(_productId, _warehouseId, _zoneId, null, initialQuantity);
    }

    [Fact]
    public void CreateReservation_ShouldReserveQuantity()
    {
        var stock = CreateStock(50m);
        var reservation = stock.CreateReservation(20m, "ORDER", _userServiceMock.Object.GetUser(default));

        reservation.Quantity.Should().Be(20m);
        stock.QuantityReserved.Should().Be(20m);
        stock.Available.Should().Be(30m);
    }

    [Fact]
    public void CreateReservation_ShouldThrow_WhenExceedingAvailable()
    {
        var stock = CreateStock(10m);
        stock.CreateReservation(10m, "ORDER", _userServiceMock.Object.GetUser(default));

        Action act = () => stock.CreateReservation(1m, "ORDER2", _userServiceMock.Object.GetUser(default));
        act.Should().Throw<InvalidOperationException>().WithMessage("*Not enough stock*");
    }

    [Fact]
    public void MultipleReservations_ShouldSumReservedCorrectly()
    {
        var stock = CreateStock(50m);
        var r1 = stock.CreateReservation(20m, "R1", _userServiceMock.Object.GetUser(default));
        var r2 = stock.CreateReservation(10m, "R2", _userServiceMock.Object.GetUser(default));

        stock.QuantityReserved.Should().Be(30m);
        stock.Available.Should().Be(20m);

        // Zwolnij pierwszą rezerwację
        stock.ReleaseReservation(r1.Id);
        stock.QuantityReserved.Should().Be(10m);
        stock.Available.Should().Be(40m);
    }

    [Fact]
    public void AdjustTotal_ShouldThrow_WhenLowerThanSumOfReservations()
    {
        var stock = CreateStock(50m);
        stock.CreateReservation(20m, "R1", _userServiceMock.Object.GetUser(default));
        stock.CreateReservation(10m, "R2", _userServiceMock.Object.GetUser(default));

        Action act = () => stock.AdjustTotal(25m); // 30 rezerwowane > 25
        act.Should().Throw<InvalidOperationException>().WithMessage("*lower than reserved*");
    }

    [Fact]
    public void AdjustTotal_ShouldWork_WhenAboveReserved()
    {
        var stock = CreateStock(50m);
        stock.CreateReservation(20m, "R1", _userServiceMock.Object.GetUser(default));
        stock.AdjustTotal(30m);

        stock.QuantityTotal.Should().Be(30m);
        stock.Available.Should().Be(10m); // 30 total - 20 reserved
    }

    [Fact]
    public void CreateReservation_ShouldThrow_WhenQuantityZeroOrNegative()
    {
        var stock = CreateStock(50m);

        Action actZero = () => stock.CreateReservation(0, "R0", _userServiceMock.Object.GetUser(default));
        actZero.Should().Throw<ArgumentException>().WithMessage("*cannot be negative*");

        Action actNegative = () => stock.CreateReservation(-5, "R-5", _userServiceMock.Object.GetUser(default));
        actNegative.Should().Throw<ArgumentException>().WithMessage("*cannot be negative*");
    }

    [Fact]
    public void EdgeQuantities_ShouldHandleMinimalAndLargeValues()
    {
        var stock = CreateStock(1_000_000m);

        var minReservation = stock.CreateReservation(0.0001m, "Min", _userServiceMock.Object.GetUser(default));
        stock.Available.Should().Be(999_999.9999m);

        var maxReservation = stock.CreateReservation(500_000m, "Max", _userServiceMock.Object.GetUser(default));
        stock.Available.Should().Be(499_999.9999m);

        stock.QuantityReserved.Should().Be(500_000.0001m);
    }

    [Fact]
    public void FulfillReservation_ShouldDecreaseTotalAndReserved()
    {
        var stock = CreateStock(100m);
        var r1 = stock.CreateReservation(30m, "R1", _userServiceMock.Object.GetUser(default));

        stock.FulfillReservation(r1.Id);

        stock.QuantityTotal.Should().Be(70m);
        stock.QuantityReserved.Should().Be(0m);
    }

    [Fact]
    public void CancelReservation_ShouldDecreaseReservedWithoutChangingTotal()
    {
        var stock = CreateStock(100m);
        var r1 = stock.CreateReservation(30m, "R1", _userServiceMock.Object.GetUser(default));

        stock.CancelReservation(r1.Id);

        stock.QuantityReserved.Should().Be(0m);
        stock.QuantityTotal.Should().Be(100m);
    }

    [Fact]
    public void ExpireReservation_ShouldOnlyExpireActive()
    {
        var stock = CreateStock(50m);
        var r1 = stock.CreateReservation(20m, "R1", _userServiceMock.Object.GetUser(default));

        r1.Release();
        stock.ExpireReservation(r1.Id);

        r1.Status.Should().Be(WarehouseManagementSystem.Domain.Enums.ReservationStatus.Released);
    }
}