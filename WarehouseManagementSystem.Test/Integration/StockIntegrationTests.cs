using System;
using FluentAssertions;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using Xunit;

namespace WarehouseManagementSystem.Tests.Integration.InventoryDomain
{
    public class StockIntegrationTests
    {
        private readonly Guid _productId = Guid.NewGuid();
        private readonly Guid _warehouseId = Guid.NewGuid();
        private readonly Guid _zoneId = Guid.NewGuid();

        private Stock CreateStock(decimal initialQuantity = 100m)
        {
            return new Stock(_productId, _warehouseId, _zoneId, null, initialQuantity);
        }

        [Fact]
        public void Constructor_ShouldInitializeStockCorrectly()
        {
            var stock = CreateStock(200m);

            stock.ProductId.Should().Be(_productId);
            stock.WarehouseId.Should().Be(_warehouseId);
            stock.WarehouseZoneId.Should().Be(_zoneId);
            stock.QuantityTotal.Should().Be(200m);
            stock.QuantityReserved.Should().Be(0m);
            stock.Available.Should().Be(200m);
        }

        [Fact]
        public void AdjustTotal_ShouldIncreaseStock_WhenAboveReserved()
        {
            var stock = CreateStock(50m);
            var reservation = stock.CreateReservation(20m, "R1", Guid.NewGuid());

            stock.AdjustTotal(80m);

            stock.QuantityTotal.Should().Be(80m);
            stock.Available.Should().Be(60m); // 80 total - 20 reserved
        }

        [Fact]
        public void AdjustTotal_ShouldThrow_WhenBelowReserved()
        {
            var stock = CreateStock(50m);
            stock.CreateReservation(30m, "R1", Guid.NewGuid());

            Action act = () => stock.AdjustTotal(20m); // < reserved
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*lower than reserved*");
        }

        [Fact]
        public void IncreaseStock_ShouldAddToTotalAndAvailable()
        {
            var stock = CreateStock(100m);

            stock.Increase(50m);

            stock.QuantityTotal.Should().Be(150m);
            stock.Available.Should().Be(150m);
        }

        [Fact]
        public void IncreaseStock_ShouldThrow_WhenNegativeOrZero()
        {
            var stock = CreateStock(100m);

            Action actZero = () => stock.Increase(0m);
            actZero.Should().Throw<ArgumentException>();

            Action actNegative = () => stock.Increase(-5m);
            actNegative.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void DecreaseStock_ShouldReduceTotalAndAvailable()
        {
            var stock = CreateStock(100m);
            stock.Decrease(30m);

            stock.QuantityTotal.Should().Be(70m);
            stock.Available.Should().Be(70m);
        }

        [Fact]
        public void DecreaseStock_ShouldThrow_WhenInsufficientAvailable()
        {
            var stock = CreateStock(50m);
            stock.CreateReservation(30m, "R1", Guid.NewGuid()); // available = 20

            Action act = () => stock.Decrease(25m); // > available
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Not enough available stock.");
        }

        [Fact]
        public void CreateReservation_ShouldReserveCorrectQuantity()
        {
            var stock = CreateStock(100m);
            var reservation = stock.CreateReservation(40m, "ORDER", Guid.NewGuid());

            reservation.Quantity.Should().Be(40m);
            stock.QuantityReserved.Should().Be(40m);
            stock.Available.Should().Be(60m);
        }

        [Fact]
        public void FulfillReservation_ShouldDecreaseTotalAndReserved()
        {
            var stock = CreateStock(100m);
            var reservation = stock.CreateReservation(30m, "R1", Guid.NewGuid());

            stock.FulfillReservation(reservation.Id);

            stock.QuantityTotal.Should().Be(70m);
            stock.QuantityReserved.Should().Be(0m);
            stock.Available.Should().Be(70m);
        }

        [Fact]
        public void CancelReservation_ShouldReleaseReservedQuantity()
        {
            var stock = CreateStock(100m);
            var reservation = stock.CreateReservation(30m, "R1", Guid.NewGuid());

            stock.CancelReservation(reservation.Id);

            stock.QuantityReserved.Should().Be(0m);
            stock.Available.Should().Be(100m);
        }

        [Fact]
        public void ExpireReservation_ShouldOnlyExpireActive()
        {
            var stock = CreateStock(50m);
            var reservation = stock.CreateReservation(20m, "R1", Guid.NewGuid());

            reservation.Release();
            stock.ExpireReservation(reservation.Id);

            reservation.Status.Should().Be(WarehouseManagementSystem.Domain.Enums.ReservationStatus.Released);
        }

        [Fact]
        public void MultipleReservations_ShouldTrackReservedAndAvailableCorrectly()
        {
            var stock = CreateStock(100m);
            var r1 = stock.CreateReservation(30m, "R1", Guid.NewGuid());
            var r2 = stock.CreateReservation(20m, "R2", Guid.NewGuid());

            stock.QuantityReserved.Should().Be(50m);
            stock.Available.Should().Be(50m);

            // Fulfill one
            stock.FulfillReservation(r1.Id);
            stock.QuantityReserved.Should().Be(20m);
            stock.QuantityTotal.Should().Be(70m);
            stock.Available.Should().Be(50m);
        }
    }
}