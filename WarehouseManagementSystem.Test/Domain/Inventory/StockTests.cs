using System;
using FluentAssertions;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using Xunit;

namespace WarehouseManagementSystem.Tests.Domain.InventoryDomain;

public class StockTests
{
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _zoneId = Guid.NewGuid();

    [Fact]
    public void Constructor_Should_Initialize_Stock_With_Correct_Values()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 10m);

        stock.QuantityTotal.Should().Be(10m);
        stock.QuantityReserved.Should().Be(0m);
        stock.Available.Should().Be(10m);
        stock.ProductId.Should().Be(_productId);
        stock.WarehouseId.Should().Be(_warehouseId);
        stock.WarehouseZoneId.Should().Be(_zoneId);
    }

    [Fact]
    public void Constructor_Should_Throw_For_Negative_InitialQuantity()
    {
        Action act = () => new Stock(_productId, _warehouseId, _zoneId, null, -1m);
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be negative*");
    }

    [Fact]
    public void Increase_Should_Add_To_Total()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 5m);
        stock.Increase(3m);

        stock.QuantityTotal.Should().Be(8m);
        stock.Available.Should().Be(8m);
    }

    [Fact]
    public void Increase_Should_Throw_For_NonPositive()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 5m);
        Action act = () => stock.Increase(0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Decrease_Should_Subtract_From_Total()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 10m);
        stock.Decrease(4m);

        stock.QuantityTotal.Should().Be(6m);
        stock.Available.Should().Be(6m);
    }

    [Fact]
    public void Decrease_Should_Throw_When_Insufficient_Available()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 5m);
        stock.CreateReservation(3m, "test", Guid.NewGuid());

        Action act = () => stock.Decrease(3m);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Not enough available stock*");
    }

    [Fact]
    public void CreateReservation_Should_Work_And_Reserve_Quantity()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 10m);
        var reservation = stock.CreateReservation(4m, "test", Guid.NewGuid());

        reservation.Quantity.Should().Be(4m);
        stock.QuantityReserved.Should().Be(4m);
        stock.Available.Should().Be(6m);
        stock.Reservations.Should().Contain(reservation);
    }

    [Fact]
    public void CreateReservation_Should_Throw_When_Exceeding_Available()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 5m);
        stock.CreateReservation(5m, "r1", Guid.NewGuid());

        Action act = () => stock.CreateReservation(1m, "r2", Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*Not enough stock*");
    }

    [Fact]
    public void ReleaseReservation_Should_Decrease_Reserved_Quantity()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 10m);
        var reservation = stock.CreateReservation(5m, "test", Guid.NewGuid());

        stock.ReleaseReservation(reservation.Id);

        stock.QuantityReserved.Should().Be(0m);
        stock.Available.Should().Be(10m);
        reservation.Status.Should().Be(WarehouseManagementSystem.Domain.Enums.ReservationStatus.Released);
    }

    [Fact]
    public void FulfillReservation_Should_Decrease_Total_And_Reserved()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 10m);
        var reservation = stock.CreateReservation(6m, "test", Guid.NewGuid());

        stock.FulfillReservation(reservation.Id);

        stock.QuantityTotal.Should().Be(4m);
        stock.QuantityReserved.Should().Be(0m);
        reservation.Status.Should().Be(WarehouseManagementSystem.Domain.Enums.ReservationStatus.Fulfilled);
    }

    [Fact]
    public void CancelReservation_Should_Decrease_Reserved_Quantity()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 10m);
        var reservation = stock.CreateReservation(4m, "test", Guid.NewGuid());

        stock.CancelReservation(reservation.Id);

        stock.QuantityReserved.Should().Be(0m);
        reservation.Status.Should().Be(WarehouseManagementSystem.Domain.Enums.ReservationStatus.Cancelled);
    }

    [Fact]
    public void ExpireReservation_Should_Decrease_Reserved_Quantity()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 8m);
        var reservation = stock.CreateReservation(3m, "test", Guid.NewGuid());

        stock.ExpireReservation(reservation.Id);

        stock.QuantityReserved.Should().Be(0m);
        reservation.Status.Should().Be(WarehouseManagementSystem.Domain.Enums.ReservationStatus.Expired);
    }

    [Fact]
    public void IsAvailable_Should_Return_Correctly()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 10m);
        stock.CreateReservation(4m, "test", Guid.NewGuid());

        stock.IsAvailable(5m).Should().BeTrue();
        stock.IsAvailable(6m).Should().BeFalse();
    }

    [Fact]
    public void AdjustTotal_Should_Change_Total()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 10m);
        stock.AdjustTotal(7m);

        stock.QuantityTotal.Should().Be(7m);
        stock.Available.Should().Be(7m);
    }

    [Fact]
    public void AdjustTotal_Should_Throw_When_Less_Than_Reserved()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 10m);
        stock.CreateReservation(5m, "test", Guid.NewGuid());

        Action act = () => stock.AdjustTotal(4m);
        act.Should().Throw<InvalidOperationException>().WithMessage("*lower than reserved*");
    }
}