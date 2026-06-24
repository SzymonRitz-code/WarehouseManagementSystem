using FluentAssertions;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Tests.Support;

namespace WarehouseManagementSystem.Tests.Domain.InventoryDomain
{
    [Trait("Category", "Inventory_Stock")]
    public class StockBehaviorTests(DomainTestFixture fixture) : IClassFixture<DomainTestFixture>
    {
        private readonly Guid _productId = Guid.NewGuid();
        private readonly Guid _warehouseId = Guid.NewGuid();
        private readonly Guid _zoneId = Guid.NewGuid();

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

        private Stock CreateStock(decimal initialQuantity = 100m)
            => new(_productId, _warehouseId, _zoneId, null, initialQuantity);

    }
}
