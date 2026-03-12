using System;
using System.Collections.Generic;
using FluentAssertions;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using Xunit;

namespace WarehouseManagementSystem.Tests.Integration.InventoryDomain;

public class ReservationIntegrationTests
{
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _zoneId = Guid.NewGuid();

    private Stock CreateStock(decimal initialQuantity = 100m)
    {
        return new Stock(_productId, _warehouseId, _zoneId, null, initialQuantity);
    }

    [Fact]
    public void CreateReservation_ShouldReserveQuantity()
    {
        var stock = CreateStock(50m);
        var reservation = stock.CreateReservation(20m, "ORDER", Guid.NewGuid());

        reservation.Quantity.Should().Be(20m);
        stock.QuantityReserved.Should().Be(20m);
        stock.Available.Should().Be(30m);
    }

    [Fact]
    public void CreateReservation_ShouldThrow_WhenExceedingAvailable()
    {
        var stock = CreateStock(10m);
        stock.CreateReservation(10m, "ORDER", Guid.NewGuid());

        Action act = () => stock.CreateReservation(1m, "ORDER2", Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*Not enough stock*");
    }

    [Fact]
    public void MultipleReservations_ShouldSumReservedCorrectly()
    {
        var stock = CreateStock(50m);
        var r1 = stock.CreateReservation(20m, "R1", Guid.NewGuid());
        var r2 = stock.CreateReservation(10m, "R2", Guid.NewGuid());

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
        stock.CreateReservation(20m, "R1", Guid.NewGuid());
        stock.CreateReservation(10m, "R2", Guid.NewGuid());

        Action act = () => stock.AdjustTotal(25m); // 30 rezerwowane > 25
        act.Should().Throw<InvalidOperationException>().WithMessage("*lower than reserved*");
    }

    [Fact]
    public void AdjustTotal_ShouldWork_WhenAboveReserved()
    {
        var stock = CreateStock(50m);
        stock.CreateReservation(20m, "R1", Guid.NewGuid());
        stock.AdjustTotal(30m);

        stock.QuantityTotal.Should().Be(30m);
        stock.Available.Should().Be(10m); // 30 total - 20 reserved
    }

    [Fact]
    public void CreateReservation_ShouldThrow_WhenQuantityZeroOrNegative()
    {
        var stock = CreateStock(50m);

        Action actZero = () => stock.CreateReservation(0, "R0", Guid.NewGuid());
        actZero.Should().Throw<ArgumentException>().WithMessage("*cannot be negative*");

        Action actNegative = () => stock.CreateReservation(-5, "R-5", Guid.NewGuid());
        actNegative.Should().Throw<ArgumentException>().WithMessage("*cannot be negative*");
    }

    [Fact]
    public void EdgeQuantities_ShouldHandleMinimalAndLargeValues()
    {
        var stock = CreateStock(1_000_000m);

        var minReservation = stock.CreateReservation(0.0001m, "Min", Guid.NewGuid());
        stock.Available.Should().Be(999_999.9999m);

        var maxReservation = stock.CreateReservation(500_000m, "Max", Guid.NewGuid());
        stock.Available.Should().Be(499_999.9999m);

        stock.QuantityReserved.Should().Be(500_000.0001m);
    }

    [Fact]
    public void FulfillReservation_ShouldDecreaseTotalAndReserved()
    {
        var stock = CreateStock(100m);
        var r1 = stock.CreateReservation(30m, "R1", Guid.NewGuid());

        stock.FulfillReservation(r1.Id);

        stock.QuantityTotal.Should().Be(70m);
        stock.QuantityReserved.Should().Be(0m);
    }

    [Fact]
    public void CancelReservation_ShouldDecreaseReservedWithoutChangingTotal()
    {
        var stock = CreateStock(100m);
        var r1 = stock.CreateReservation(30m, "R1", Guid.NewGuid());

        stock.CancelReservation(r1.Id);

        stock.QuantityReserved.Should().Be(0m);
        stock.QuantityTotal.Should().Be(100m);
    }

    [Fact]
    public void ExpireReservation_ShouldOnlyExpireActive()
    {
        var stock = CreateStock(50m);
        var r1 = stock.CreateReservation(20m, "R1", Guid.NewGuid());

        r1.Release();
        stock.ExpireReservation(r1.Id);

        r1.Status.Should().Be(WarehouseManagementSystem.Domain.Enums.ReservationStatus.Released);
    }
}