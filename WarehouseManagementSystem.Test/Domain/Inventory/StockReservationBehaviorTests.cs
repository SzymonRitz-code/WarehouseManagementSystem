using FluentAssertions;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Tests.Support;

namespace WarehouseManagementSystem.Tests.Domain.InventoryDomain;

/// <summary>
/// Tests for the <see cref="Stock"/> class in the Inventory domain, focusing on stock reservation behaviors.
/// </summary>
/// <param name="fixture">The domain test fixture used for setting up test dependencies.</param>
[Trait("Category", "Inventory_StockReservation")]
public class StockReservationBehaviorTests(DomainTestFixture fixture) : IClassFixture<DomainTestFixture>
{
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _zoneId = Guid.NewGuid();

    /// <summary>
    /// Test to ensure that creating a reservation correctly reserves the specified quantity and updates the stock's reserved and available quantities accordingly.
    /// </summary>
    [Fact]
    public void CreateReservation_ShouldReserveQuantity()
    {
        // Arrange
        var stock = CreateStock(50m);

        // Act
        var reservation = stock.CreateReservation(20m, "ORDER", fixture.User);

        // Assert
        reservation.Quantity.Should().Be(20m);
        stock.QuantityReserved.Should().Be(20m);
        stock.Available.Should().Be(30m);
    }

    /// <summary>
    /// Test to ensure that attempting to create a reservation that exceeds the available stock throws an InvalidOperationException.
    /// </summary>
    [Fact]
    public void CreateReservation_ShouldThrow_WhenExceedingAvailable()
    {
        // Arrange
        var stock = CreateStock(10m);
        stock.CreateReservation(10m, "ORDER", fixture.User);

        // Act
        Action act = () => stock.CreateReservation(1m, "ORDER2", fixture.User);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*Not enough stock*");
    }

    /// <summary>
    /// Test to ensure that releasing a reservation correctly updates the stock's reserved and available quantities.
    /// </summary>
    [Fact]
    public void MultipleReservations_ShouldSumReservedCorrectly()
    {
        // Arrange
        var stock = CreateStock(50m);
        var r1 = stock.CreateReservation(20m, "R1", fixture.User);
        stock.CreateReservation(10m, "R2", fixture.User);

        // Act
        stock.ReleaseReservation(r1.Id);

        // Assert
        stock.QuantityReserved.Should().Be(10m);
        stock.Available.Should().Be(40m);
    }

    /// <summary>
    /// Test to ensure that adjusting the total stock quantity to a value lower than the sum of active reservations throws an InvalidOperationException.
    /// </summary>
    [Fact]
    public void AdjustTotal_ShouldThrow_WhenLowerThanSumOfReservations()
    {
        // Arrange
        var stock = CreateStock(50m);
        stock.CreateReservation(20m, "R1", fixture.User);
        stock.CreateReservation(10m, "R2", fixture.User);

        // Act
        Action act = () => stock.AdjustTotal(25m);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*lower than reserved*");
    }

    /// <summary>
    /// Test to ensure that adjusting the total stock quantity to a value above the sum of active reservations correctly updates the stock's total and available quantities.
    /// </summary>
    [Fact]
    public void AdjustTotal_ShouldWork_WhenAboveReserved()
    {
        // Arrange
        var stock = CreateStock(50m);
        stock.CreateReservation(20m, "R1", fixture.User);

        // Act
        stock.AdjustTotal(30m);

        // Assert
        stock.QuantityTotal.Should().Be(30m);
        stock.Available.Should().Be(10m);
    }

    /// <summary>
    /// Test to ensure that creating a reservation with a zero or negative quantity throws an ArgumentException, 
    /// validating that only positive quantities are allowed for reservations.
    /// </summary>
    /// <param name="quantity">The quantity to test for reservation creation.</param>
    [Theory]
    [ClassData(typeof(InvalidPositiveDecimalTestData))]
    public void CreateReservation_ShouldThrow_WhenQuantityZeroOrNegative(decimal quantity)
    {
        // Arrange
        var stock = CreateStock(50m);

        // Act
        Action act = () => stock.CreateReservation(quantity, "R0", fixture.User);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*cannot be negative*");
    }
    /// <summary>
    /// Test to ensure that the system can handle edge cases for reservation quantities, including very small and very large values, 
    /// without losing precision or causing errors.
    /// </summary>
    [Fact]
    public void EdgeQuantities_ShouldHandleMinimalAndLargeValues()
    {
        // Arrange
        var stock = CreateStock(1_000_000m);

        // Act
        stock.CreateReservation(0.0001m, "Min", fixture.User);
        stock.CreateReservation(500_000m, "Max", fixture.User);

        // Assert
        stock.Available.Should().Be(499_999.9999m);
        stock.QuantityReserved.Should().Be(500_000.0001m);
    }

    /// <summary>
    /// Test to ensure that fulfilling a reservation correctly decreases both the total stock quantity and the reserved quantity, 
    /// reflecting the fulfillment of the reserved items.
    /// </summary>
    [Fact]
    public void FulfillReservation_ShouldDecreaseTotalAndReserved()
    {
        // Arrange
        var stock = CreateStock(100m);
        var r1 = stock.CreateReservation(30m, "R1", fixture.User);

        // Act
        stock.FulfillReservation(r1.Id);

        // Assert
        stock.QuantityTotal.Should().Be(70m);
        stock.QuantityReserved.Should().Be(0m);
    }

    /// <summary>
    /// Test to ensure that canceling a reservation correctly decreases the reserved quantity without affecting the total stock quantity,
    /// </summary>
    [Fact]
    public void CancelReservation_ShouldDecreaseReservedWithoutChangingTotal()
    {
        // Arrange
        var stock = CreateStock(100m);
        var r1 = stock.CreateReservation(30m, "R1", fixture.User);

        // Act
        stock.CancelReservation(r1.Id);

        // Assert
        stock.QuantityReserved.Should().Be(0m);
        stock.QuantityTotal.Should().Be(100m);
    }

    /// <summary>
    /// Test to ensure that expiring a reservation only affects active reservations and does not change the status of already released reservations.
    /// </summary>
    [Fact]
    public void ExpireReservation_ShouldOnlyExpireActive()
    {
        // Arrange
        var stock = CreateStock(50m);
        var r1 = stock.CreateReservation(20m, "R1", fixture.User);

        // Act
        r1.Release();
        stock.ExpireReservation(r1.Id);

        // Assert
        r1.Status.Should().Be(WarehouseManagementSystem.Domain.Enums.ReservationStatus.Released);
    }

    /// <summary>
    /// Test to ensure that creating a stock instance with a specified initial quantity correctly sets the total and available quantities,
    /// </summary>
    /// <param name="initialQuantity">The initial quantity to set for the stock instance.</param>
    /// <returns>A new stock instance with the specified initial quantity.</returns>
    private Stock CreateStock(decimal initialQuantity = 100m)
    {
        return new(_productId, _warehouseId, _zoneId, null, initialQuantity);
    }
}
