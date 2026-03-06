using FluentAssertions;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.Tests.Domain.InventoryDomain;

public class StockReservationTests
{
    private readonly Guid _stockId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private StockReservation CreateReservation(
        decimal quantity = 10,
        string source = "ORDER",
        DateTimeOffset? expires = null)
    {
        return new StockReservation(
            _stockId,
            quantity,
            source,
            _userId,
            expires);
    }

    [Fact]
    public void Constructor_Should_CreateReservation_WithValidData()
    {
        var reservation = CreateReservation();

        reservation.Quantity.Should().Be(10);
        reservation.ReservationSource.Should().Be("ORDER");
        reservation.Status.Should().Be(ReservationStatus.Active);
        reservation.StockId.Should().Be(_stockId);
        reservation.CreatedBy.Should().Be(_userId);
    }

    [Fact]
    public void Constructor_Should_Throw_WhenQuantityIsZero()
    {
        Action act = () => CreateReservation(0);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public void Constructor_Should_Throw_WhenQuantityIsNegative()
    {
        Action act = () => CreateReservation(-5);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetReservationSource_Should_SetSource_WhenValid()
    {
        var reservation = CreateReservation();

        reservation.SetReservationSource("TRANSFER");

        reservation.ReservationSource.Should().Be("TRANSFER");
    }

    [Fact]
    public void SetReservationSource_Should_Throw_WhenEmpty()
    {
        var reservation = CreateReservation();

        Action act = () => reservation.SetReservationSource("");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*required*");
    }

    [Fact]
    public void SetReservationSource_Should_Throw_WhenTooLong()
    {
        var reservation = CreateReservation();

        var longSource = new string('A', 51);

        Action act = () => reservation.SetReservationSource(longSource);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*too long*");
    }

    [Fact]
    public void Increase_Should_AddQuantity()
    {
        var reservation = CreateReservation();

        reservation.Increase(5);

        reservation.Quantity.Should().Be(15);
    }

    [Fact]
    public void Increase_Should_Throw_WhenQuantityNegative()
    {
        var reservation = CreateReservation();

        Action act = () => reservation.Increase(-1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Decrease_Should_SubtractQuantity()
    {
        var reservation = CreateReservation();

        reservation.Decrease(3);

        reservation.Quantity.Should().Be(7);
    }

    [Fact]
    public void Decrease_Should_Throw_WhenTooMuch()
    {
        var reservation = CreateReservation();

        Action act = () => reservation.Decrease(20);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*more than reserved*");
    }

    [Fact]
    public void Release_Should_SetStatusReleased()
    {
        var reservation = CreateReservation();

        reservation.Release();

        reservation.Status.Should().Be(ReservationStatus.Released);
    }

    [Fact]
    public void Fulfill_Should_SetStatusFulfilled()
    {
        var reservation = CreateReservation();

        reservation.Fulfill();

        reservation.Status.Should().Be(ReservationStatus.Fulfilled);
    }

    [Fact]
    public void Cancel_Should_SetStatusCancelled()
    {
        var reservation = CreateReservation();

        reservation.Cancel();

        reservation.Status.Should().Be(ReservationStatus.Cancelled);
    }

    [Fact]
    public void Expire_Should_SetStatusExpired_WhenActive()
    {
        var reservation = CreateReservation();

        reservation.Expire();

        reservation.Status.Should().Be(ReservationStatus.Expired);
    }

    [Fact]
    public void Expire_Should_DoNothing_WhenNotActive()
    {
        var reservation = CreateReservation();

        reservation.Release();

        reservation.Expire();

        reservation.Status.Should().Be(ReservationStatus.Released);
    }

    [Fact]
    public void IsExpired_Should_ReturnFalse_WhenNoExpiration()
    {
        var reservation = CreateReservation();

        reservation.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_Should_ReturnTrue_WhenDatePassed()
    {
        var reservation = CreateReservation(
            expires: DateTimeOffset.UtcNow.AddMinutes(-1));

        reservation.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void SetExpiration_Should_SetDate_WhenValid()
    {
        var reservation = CreateReservation();

        var expires = DateTimeOffset.UtcNow.AddHours(1);

        reservation.SetExpiration(expires);

        reservation.ExpiresAt.Should().Be(expires);
    }

    [Fact]
    public void SetExpiration_Should_Throw_WhenEarlierThanCreated()
    {
        var reservation = CreateReservation();

        var invalid = reservation.CreatedAt.AddMinutes(-1);

        Action act = () => reservation.SetExpiration(invalid);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*later than creation*");
    }
}