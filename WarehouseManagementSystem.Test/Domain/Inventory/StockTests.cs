using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Tests.Domain.InventoryDomain;

/// <summary>
/// Tests for the <see cref="Stock"/> class in the Inventory domain, focusing on stock behaviors such as increasing, decreasing, and managing reservations.
/// </summary>
public class StockTests
{
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _zoneId = Guid.NewGuid();
    private readonly Mock<IUserService> _userServiceMock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="StockTests"/> class and sets up
    /// </summary>
    public StockTests()
    {
        _userServiceMock.Setup(s => s.GetUser(It.IsAny<HttpContext>()))
            .Returns(new UserSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Testomir.Testowski@gmail.com", "Testomir"));
    }
    /// <summary>
    /// Tests that the constructor of the <see cref="Stock"/> class initializes the stock with the correct values.
    /// </summary>
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
    /// <summary>
    /// Tests that the constructor of the <see cref="Stock"/> class throws an exception when initialized with a negative initial quantity.
    /// </summary>
    [Fact]
    public void Constructor_Should_Throw_For_Negative_InitialQuantity()
    {
        Action act = () => new Stock(_productId, _warehouseId, _zoneId, null, -1m);
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be negative*");
    }
    /// <summary>
    /// Tests that the Increase method of the <see cref="Stock"/> class correctly adds to the total quantity and updates the available quantity.
    /// </summary>
    [Fact]
    public void Increase_Should_Add_To_Total()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 5m);
        stock.Increase(3m);

        stock.QuantityTotal.Should().Be(8m);
        stock.Available.Should().Be(8m);
    }
    /// <summary>
    /// Tests that the Increase method of the <see cref="Stock"/> class throws an exception when a non-positive quantity is provided.
    /// </summary>
    [Fact]
    public void Increase_Should_Throw_For_NonPositive()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 5m);
        Action act = () => stock.Increase(0);
        act.Should().Throw<ArgumentException>();
    }
    /// <summary>
    /// Tests that the Decrease method of the <see cref="Stock"/> class correctly subtracts from the total quantity and updates the available quantity.
    /// </summary>
    [Fact]
    public void Decrease_Should_Subtract_From_Total()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 10m);
        stock.Decrease(4m);

        stock.QuantityTotal.Should().Be(6m);
        stock.Available.Should().Be(6m);
    }
    /// <summary>
    /// Tests that the Decrease method of the <see cref="Stock"/> class throws an exception when a non-positive quantity is provided.
    /// </summary>
    [Fact]
    public void Decrease_Should_Throw_When_Insufficient_Available()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 5m);
        stock.CreateReservation(3m, "test", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));

        Action act = () => stock.Decrease(3m);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Not enough available stock*");
    }
    /// <summary>
    /// Tests that the CreateReservation method of the <see cref="Stock"/> class correctly creates a reservation and updates the reserved quantity.
    /// </summary>
    [Fact]
    public void CreateReservation_Should_Work_And_Reserve_Quantity()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 10m);
        var reservation = stock.CreateReservation(4m, "test", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));

        reservation.Quantity.Should().Be(4m);
        stock.QuantityReserved.Should().Be(4m);
        stock.Available.Should().Be(6m);
        stock.Reservations.Should().Contain(reservation);
    }
    /// <summary>
    /// Tests that the CreateReservation method of the <see cref="Stock"/> class throws an exception when trying to reserve more than the available quantity.
    /// </summary>
    [Fact]
    public void CreateReservation_Should_Throw_When_Exceeding_Available()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 5m);
        stock.CreateReservation(5m, "r1", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));

        Action act = () => stock.CreateReservation(1m, "r2", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        act.Should().Throw<InvalidOperationException>().WithMessage("*Not enough stock*");
    }

    /// <summary>
    /// Tests that the ReleaseReservation method of the <see cref="Stock"/> class correctly releases a reservation and updates the reserved quantity.
    /// </summary>
    [Fact]
    public void ReleaseReservation_Should_Decrease_Reserved_Quantity()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 10m);
        var reservation = stock.CreateReservation(5m, "test", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));

        stock.ReleaseReservation(reservation.Id);

        stock.QuantityReserved.Should().Be(0m);
        stock.Available.Should().Be(10m);
        reservation.Status.Should().Be(WarehouseManagementSystem.Domain.Enums.ReservationStatus.Released);
    }

    /// <summary>
    /// Tests that the FulfillReservation method of the <see cref="Stock"/> class correctly fulfills a reservation, decreasing both total and reserved quantities.
    /// </summary>
    [Fact]
    public void FulfillReservation_Should_Decrease_Total_And_Reserved()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 10m);
        var reservation = stock.CreateReservation(6m, "test", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));

        stock.FulfillReservation(reservation.Id);

        stock.QuantityTotal.Should().Be(4m);
        stock.QuantityReserved.Should().Be(0m);
        reservation.Status.Should().Be(WarehouseManagementSystem.Domain.Enums.ReservationStatus.Fulfilled);
    }

    /// <summary>
    /// Tests that the CancelReservation method of the <see cref="Stock"/> class correctly cancels a reservation and updates the reserved quantity.
    /// </summary>
    [Fact]
    public void CancelReservation_Should_Decrease_Reserved_Quantity()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 10m);
        var reservation = stock.CreateReservation(4m, "test", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));

        stock.CancelReservation(reservation.Id);

        stock.QuantityReserved.Should().Be(0m);
        reservation.Status.Should().Be(WarehouseManagementSystem.Domain.Enums.ReservationStatus.Cancelled);
    }

    /// <summary>
    /// Tests that the ExpireReservation method of the <see cref="Stock"/> class correctly expires a reservation and updates the reserved quantity.
    /// </summary>
    [Fact]
    public void ExpireReservation_Should_Decrease_Reserved_Quantity()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 8m);
        var reservation = stock.CreateReservation(3m, "test", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));

        stock.ExpireReservation(reservation.Id);

        stock.QuantityReserved.Should().Be(0m);
        reservation.Status.Should().Be(WarehouseManagementSystem.Domain.Enums.ReservationStatus.Expired);
    }

    /// <summary>
    /// Tests that the IsAvailable method of the <see cref="Stock"/> class correctly determines if a specified quantity is available for reservation or fulfillment.
    /// </summary>
    [Fact]
    public void IsAvailable_Should_Return_Correctly()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 10m);
        stock.CreateReservation(4m, "test", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));

        stock.IsAvailable(6m).Should().BeTrue();
        stock.IsAvailable(5m).Should().BeTrue();
        stock.IsAvailable(7m).Should().BeFalse();
    }

    /// <summary>
    /// Tests that the AdjustTotal method of the <see cref="Stock"/> class correctly adjusts the total quantity and updates the available quantity.
    /// </summary>
    [Fact]
    public void AdjustTotal_Should_Change_Total()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 10m);
        stock.AdjustTotal(7m);

        stock.QuantityTotal.Should().Be(7m);
        stock.Available.Should().Be(7m);
    }

    /// <summary>
    /// Tests that the AdjustTotal method of the <see cref="Stock"/> class throws an exception when attempting 
    /// to set the total quantity to a value less than the reserved quantity.
    /// </summary>
    [Fact]
    public void AdjustTotal_Should_Throw_When_Less_Than_Reserved()
    {
        var stock = new Stock(_productId, _warehouseId, _zoneId, null, 10m);
        stock.CreateReservation(5m, "test", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));

        Action act = () => stock.AdjustTotal(4m);
        act.Should().Throw<InvalidOperationException>().WithMessage("*lower than reserved*");
    }
}