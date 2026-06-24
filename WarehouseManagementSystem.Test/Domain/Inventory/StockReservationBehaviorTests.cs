using FluentAssertions;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Tests.Support;

namespace WarehouseManagementSystem.Tests.Domain.InventoryDomain;

[Trait("Category", "Inventory_StockReservation")]
public class StockReservationBehaviorTests(DomainTestFixture fixture) : IClassFixture<DomainTestFixture>
{
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _zoneId = Guid.NewGuid();

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

    private Stock CreateStock(decimal initialQuantity = 100m)
        => new(_productId, _warehouseId, _zoneId, null, initialQuantity);
}
