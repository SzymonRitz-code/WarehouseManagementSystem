using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.ValueObjects;
using WarehouseManagementSystem.Infrastructure.Services;

namespace WarehouseManagementSystem.Tests.Domain.InventoryDomain;

/// <summary>
/// Tests for the <see cref="StockReservation"/> class in the domain model, focusing on its behavior and state transitions.
/// </summary>
public class StockReservationTests
{
    private readonly Guid _stockId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<ISystemClock> _clockMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();

    public StockReservationTests()
    {
        _userServiceMock.Setup(s => s.GetUser(It.IsAny<HttpContext>()))
            .Returns(new UserSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Testomir.Testowski@gmail.com", "Testomir"));
    }

    #region Helper Methods

    /// <summary>
    /// Creates a new instance of <see cref="StockReservation"/> with the specified parameters for testing purposes.
    /// </summary>
    /// <param name="quantity">The quantity to set for the reservation.</param>
    /// <param name="source">The source of the reservation.</param>
    /// <param name="expires">The expiration date of the reservation.</param>
    /// <returns>A new instance of <see cref="StockReservation"/> with the specified parameters.</returns>
    private StockReservation CreateReservation(
        decimal quantity = 10,
        string source = "ORDER",
        DateTimeOffset? expires = null)
    {
        return new StockReservation(
            _stockId,
            quantity,
            source,
            _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()),
            expires);
    }

    #endregion

    #region Constructor and Validation Tests

    /// <summary>
    /// Tests the constructor of the <see cref="StockReservation"/> class to ensure it creates a reservation with valid data.
    /// </summary>
    [Fact]
    public void Constructor_Should_CreateReservation_WithValidData()
    {
        var reservation = CreateReservation();

        reservation.Quantity.Should().Be(10);
        reservation.ReservationSource.Should().Be("ORDER");
        reservation.Status.Should().Be(ReservationStatus.Active);
        reservation.StockId.Should().Be(_stockId);
        reservation.CreatedByUser.Should().Be(_userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
    }

    /// <summary>
    /// Tests the constructor of the <see cref="StockReservation"/> class to ensure it throws an exception when the quantity is zero.
    /// </summary>
    /// 
    [Fact]
    public void Constructor_Should_Throw_WhenQuantityIsZero()
    {
        Action act = () => CreateReservation(0);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*greater than zero*");
    }

    /// <summary>
    /// Tests the constructor of the <see cref="StockReservation"/> class to ensure it throws an exception when the quantity is negative.
    /// </summary>
    /// 
    [Fact]
    public void Constructor_Should_Throw_WhenQuantityIsNegative()
    {
        Action act = () => CreateReservation(-5);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests the constructor of the <see cref="StockReservation"/> class to ensure it throws an exception when the reservation source is empty.
    /// </summary>
    [Fact]
    public void SetReservationSource_Should_SetSource_WhenValid()
    {
        var reservation = CreateReservation();

        reservation.SetReservationSource("TRANSFER");

        reservation.ReservationSource.Should().Be("TRANSFER");
    }

    /// <summary>
    /// Tests the constructor of the <see cref="StockReservation"/> class to ensure it throws an exception when the reservation source is empty.
    /// </summary>
    [Fact]
    public void SetReservationSource_Should_Throw_WhenEmpty()
    {
        var reservation = CreateReservation();

        Action act = () => reservation.SetReservationSource("");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*required*");
    }

    /// <summary>
    /// Tests the constructor of the <see cref="StockReservation"/> class to ensure it throws an exception when the reservation source exceeds the maximum length.
    /// </summary>
    [Fact]
    public void SetReservationSource_Should_Throw_WhenTooLong()
    {
        var reservation = CreateReservation();

        var longSource = new string('A', 51);

        Action act = () => reservation.SetReservationSource(longSource);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*too long*");
    }

    #endregion

    #region Quantity Modification Tests

    /// <summary>
    /// Tests the Increase method of the <see cref="StockReservation"/> class to ensure it correctly adds to the quantity.
    /// </summary>
    [Fact]
    public void Increase_Should_AddQuantity()
    {
        var reservation = CreateReservation();

        reservation.Increase(5);

        reservation.Quantity.Should().Be(15);
    }

    /// <summary>
    /// Tests the Increase method of the <see cref="StockReservation"/> class to ensure it throws an exception when a negative quantity is provided.
    /// </summary>
    [Fact]
    public void Increase_Should_Throw_WhenQuantityNegative()
    {
        var reservation = CreateReservation();

        Action act = () => reservation.Increase(-1);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests the Decrease method of the <see cref="StockReservation"/> class to ensure it correctly subtracts from the quantity.
    /// </summary>
    [Fact]
    public void Decrease_Should_SubtractQuantity()
    {
        var reservation = CreateReservation();

        reservation.Decrease(3);

        reservation.Quantity.Should().Be(7);
    }

    /// <summary>
    /// Tests the Decrease method of the <see cref="StockReservation"/> class to ensure it throws an exception when a negative quantity is provided.
    /// </summary>
    [Fact]
    public void Decrease_Should_Throw_WhenTooMuch()
    {
        var reservation = CreateReservation();

        Action act = () => reservation.Decrease(20);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*more than reserved*");
    }

    #endregion

    #region Status Transition Tests

    /// <summary>
    /// Tests the Release method of the <see cref="StockReservation"/> class to ensure it correctly sets the status to Released.
    /// </summary>
    [Fact]
    public void Release_Should_SetStatusReleased()
    {
        var reservation = CreateReservation();

        reservation.Release();

        reservation.Status.Should().Be(ReservationStatus.Released);
    }

    /// <summary>
    /// Tests the Fulfill method of the <see cref="StockReservation"/> class to ensure it correctly sets the status to Fulfilled.
    /// </summary>
    [Fact]
    public void Fulfill_Should_SetStatusFulfilled()
    {
        var reservation = CreateReservation();

        reservation.Fulfill();

        reservation.Status.Should().Be(ReservationStatus.Fulfilled);
    }

    /// <summary>
    /// Tests the Cancel method of the <see cref="StockReservation"/> class to ensure it correctly sets the status to Cancelled.
    /// </summary>
    [Fact]
    public void Cancel_Should_SetStatusCancelled()
    {
        var reservation = CreateReservation();

        reservation.Cancel();

        reservation.Status.Should().Be(ReservationStatus.Cancelled);
    }

    /// <summary>
    /// Tests the Expire method of the <see cref="StockReservation"/> class to ensure it correctly sets the status to Expired when the reservation is active.
    /// </summary>
    [Fact]
    public void Expire_Should_SetStatusExpired_WhenActive()
    {
        var reservation = CreateReservation();

        reservation.Expire();

        reservation.Status.Should().Be(ReservationStatus.Expired);
    }

    /// <summary>
    /// Tests the Expire method of the <see cref="StockReservation"/> class to ensure it does nothing when the reservation is not active (e.g., already released).
    /// </summary>
    [Fact]
    public void Expire_Should_DoNothing_WhenNotActive()
    {
        var reservation = CreateReservation();

        reservation.Release();

        reservation.Expire();

        reservation.Status.Should().Be(ReservationStatus.Released);
    }

    #endregion

    #region Expiration Tests

    /// <summary>
    /// Tests the IsExpired method of the <see cref="StockReservation"/> class to ensure it returns false when there is no expiration date set.
    /// </summary>
    [Fact]
    public void IsExpired_Should_ReturnFalse_WhenNoExpiration()
    {
        var reservation = CreateReservation();

        reservation.IsExpired(_clockMock.Object.UtcNow).Should().BeFalse();
    }

    /// <summary>
    /// Tests the IsExpired method of the <see cref="StockReservation"/> class to ensure it returns false when the expiration date has not yet passed.
    /// </summary>
    [Fact]
    public void IsExpired_Should_ReturnTrue_WhenDatePassed()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(1);

        var reservation = new StockReservation(
            _stockId,
            quantity: 10,
            reservationSource: "ORDER",
            createdByUser: _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()),
            expiresAt: expiresAt); // expiration po CreatedAt
        var now = DateTimeOffset.UtcNow;
        // Act
        var result = reservation.IsExpired(now.AddSeconds(1));

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests the SetExpiration method of the <see cref="StockReservation"/> class to ensure it correctly sets the expiration date when a valid date is provided.
    /// </summary>
    [Fact]
    public void SetExpiration_Should_SetDate_WhenValid()
    {
        var reservation = CreateReservation();

        var expires = DateTimeOffset.UtcNow.AddHours(1);

        reservation.SetExpiration(expires);

        reservation.ExpiresAt.Should().Be(expires);
    }

    /// <summary>
    /// Tests the SetExpiration method of the <see cref="StockReservation"/> class to ensure it throws an exception 
    /// when the provided expiration date is earlier than the creation date.
    /// </summary>
    [Fact]
    public void SetExpiration_Should_Throw_WhenEarlierThanCreated()
    {
        var reservation = CreateReservation();

        var invalid = reservation.CreatedAt.AddMinutes(-1);

        Action act = () => reservation.SetExpiration(invalid);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*later than creation*");
    }

    #endregion
}