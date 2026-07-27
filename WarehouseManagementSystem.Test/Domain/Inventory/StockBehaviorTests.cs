using FluentAssertions;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Tests.Support;

namespace WarehouseManagementSystem.Tests.Domain.InventoryDomain
{
    /// <summary>
    /// Tests for the <see cref="Stock"/> class in the Inventory domain, focusing on stock behaviors such as adjusting total, increasing, decreasing, and managing reservations.
    /// </summary>
    /// <param name="fixture">The domain test fixture used for setting up test dependencies.</param>
    [Trait("Category", "Inventory_Stock")]
    public class StockBehaviorTests(DomainTestFixture fixture) : IClassFixture<DomainTestFixture>
    {
        private readonly Guid _productId = Guid.NewGuid();
        private readonly Guid _warehouseId = Guid.NewGuid();
        private readonly Guid _zoneId = Guid.NewGuid();
        /// <summary>
        /// Tests that the constructor of the <see cref="Stock"/> class initializes properties correctly.
        /// </summary>
        [Fact]
        public void Constructor_ShouldInitializeStockCorrectly()
        {
            // Arrange
            var stock = CreateStock(200m);

            // Act
            var available = stock.Available;

            // Assert
            stock.ProductId.Should().Be(_productId);
            stock.WarehouseId.Should().Be(_warehouseId);
            stock.WarehouseZoneId.Should().Be(_zoneId);
            stock.QuantityTotal.Should().Be(200m);
            stock.QuantityReserved.Should().Be(0m);
            available.Should().Be(200m);
        }

        /// <summary>
        /// Tests that the AdjustTotal method correctly increases the total stock when the new total is above the reserved quantity.
        /// </summary>
        [Fact]
        public void AdjustTotal_ShouldIncreaseStock_WhenAboveReserved()
        {
            // Arrange
            var stock = CreateStock(50m);
            stock.CreateReservation(20m, "R1", fixture.User);

            // Act
            stock.AdjustTotal(80m);

            // Assert
            stock.QuantityTotal.Should().Be(80m);
            stock.Available.Should().Be(60m);
        }

        /// <summary>
        /// Tests that the AdjustTotal method throws an InvalidOperationException when attempting to set the total stock below the reserved quantity.
        /// </summary>
        [Fact]
        public void AdjustTotal_ShouldThrow_WhenBelowReserved()
        {
            // Arrange
            var stock = CreateStock(50m);
            stock.CreateReservation(30m, "R1", fixture.User);

            // Act
            Action act = () => stock.AdjustTotal(20m);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*lower than reserved*");
        }

        /// <summary>
        /// Tests that the Increase method correctly adds to the total and available stock.
        /// </summary>
        [Fact]
        public void IncreaseStock_ShouldAddToTotalAndAvailable()
        {
            // Arrange
            var stock = CreateStock(100m);

            // Act
            stock.Increase(50m);

            // Assert
            stock.QuantityTotal.Should().Be(150m);
            stock.Available.Should().Be(150m);
        }

        /// <summary>
        /// Tests that the Increase method throws an ArgumentException when attempting to increase stock with a negative or zero quantity.
        /// </summary>
        /// <param name="quantity"></param>
        [Theory]
        [ClassData(typeof(InvalidPositiveDecimalTestData))]
        public void IncreaseStock_ShouldThrow_WhenNegativeOrZero(decimal quantity)
        {
            // Arrange
            var stock = CreateStock(100m);

            // Act
            Action act = () => stock.Increase(quantity);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Tests that the Decrease method correctly reduces the total and available stock when sufficient stock is available.
        /// </summary>
        [Fact]
        public void DecreaseStock_ShouldReduceTotalAndAvailable()
        {
            // Arrange
            var stock = CreateStock(100m);

            // Act
            stock.Decrease(30m);

            // Assert
            stock.QuantityTotal.Should().Be(70m);
            stock.Available.Should().Be(70m);
        }

        /// <summary>
        /// Tests that the Decrease method throws an InvalidOperationException when attempting to decrease stock below the available quantity, considering reserved stock.
        /// </summary>
        [Fact]
        public void DecreaseStock_ShouldThrow_WhenInsufficientAvailable()
        {
            // Arrange
            var stock = CreateStock(50m);
            stock.CreateReservation(30m, "R1", fixture.User);

            // Act
            Action act = () => stock.Decrease(25m);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Not enough available stock.");
        }

        /// <summary>
        /// Tests that the CreateReservation method correctly reserves the specified quantity and updates the reserved and available stock accordingly.
        /// </summary>
        [Fact]
        public void CreateReservation_ShouldReserveCorrectQuantity()
        {
            // Arrange
            var stock = CreateStock(100m);

            // Act
            var reservation = stock.CreateReservation(40m, "ORDER", fixture.User);

            // Assert
            reservation.Quantity.Should().Be(40m);
            stock.QuantityReserved.Should().Be(40m);
            stock.Available.Should().Be(60m);
        }

        /// <summary>
        /// Tests that the FulfillReservation method correctly decreases the total stock and releases the reserved quantity when a reservation is fulfilled.
        /// </summary>
        [Fact]
        public void FulfillReservation_ShouldDecreaseTotalAndReserved()
        {
            // Arrange
            var stock = CreateStock(100m);
            var reservation = stock.CreateReservation(30m, "R1", fixture.User);

            // Act
            stock.FulfillReservation(reservation.Id);

            // Assert
            stock.QuantityTotal.Should().Be(70m);
            stock.QuantityReserved.Should().Be(0m);
            stock.Available.Should().Be(70m);
        }

        /// <summary>
        /// Tests that the CancelReservation method correctly releases the reserved quantity and updates the reserved and available stock accordingly.
        /// </summary>
        [Fact]
        public void CancelReservation_ShouldReleaseReservedQuantity()
        {
            // Arrange
            var stock = CreateStock(100m);
            var reservation = stock.CreateReservation(30m, "R1", fixture.User);

            // Act
            stock.CancelReservation(reservation.Id);

            // Assert
            stock.QuantityReserved.Should().Be(0m);
            stock.Available.Should().Be(100m);
        }

        /// <summary>
        /// Tests that the ExpireReservation method only expires active reservations and does not affect released reservations.
        /// </summary>
        [Fact]
        public void ExpireReservation_ShouldOnlyExpireActive()
        {
            // Arrange
            var stock = CreateStock(50m);
            var reservation = stock.CreateReservation(20m, "R1", fixture.User);

            // Act
            reservation.Release();
            stock.ExpireReservation(reservation.Id);

            // Assert
            reservation.Status.Should().Be(WarehouseManagementSystem.Domain.Enums.ReservationStatus.Released);
        }

        /// <summary>
        /// Tests that multiple reservations are tracked correctly, and fulfilling one reservation updates the reserved and available quantities as expected.
        /// </summary>
        [Fact]
        public void MultipleReservations_ShouldTrackReservedAndAvailableCorrectly()
        {
            // Arrange
            var stock = CreateStock(100m);
            var r1 = stock.CreateReservation(30m, "R1", fixture.User);
            stock.CreateReservation(20m, "R2", fixture.User);

            // Act
            stock.QuantityReserved.Should().Be(50m);
            stock.Available.Should().Be(50m);
            stock.FulfillReservation(r1.Id);

            // Assert
            stock.QuantityReserved.Should().Be(20m);
            stock.QuantityTotal.Should().Be(70m);
            stock.Available.Should().Be(50m);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Stock"/> class with the specified initial quantity for testing purposes.
        /// </summary>
        /// <param name="initialQuantity">The initial quantity of the stock.</param>
        /// <returns>A new instance of the <see cref="Stock"/> class.</returns>
        private Stock CreateStock(decimal initialQuantity = 100m)
        {
            return new(_productId, _warehouseId, _zoneId, null, initialQuantity);
        }
    }
}
