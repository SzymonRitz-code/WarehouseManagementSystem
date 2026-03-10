using FluentAssertions;
using Moq;
using WarehouseManagementSystem.API.Services.Stocks;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Infrastructure.Services;
using Xunit;

namespace WarehouseManagementSystem.Tests.Services
{
    public class StockServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ISystemClock> _clockMock = new();
        private readonly StockService _service;

        public StockServiceTests()
        {
            _service = new StockService(_unitOfWorkMock.Object, _clockMock.Object);
        }

        #region GetOrCreateAsync Tests

        [Fact]
        public async Task GetOrCreateAsync_ShouldReturnExistingStock()
        {
            var stock = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 10m);
            _unitOfWorkMock.Setup(u => u.Stocks.GetByProductAndWarehouseAsync(
                stock.ProductId, stock.WarehouseId, stock.WarehouseZoneId, null))
                .ReturnsAsync(stock);

            var result = await _service.GetOrCreateAsync(stock.ProductId, stock.WarehouseId, stock.WarehouseZoneId, null);

            result.Should().Be(stock);
            _unitOfWorkMock.Verify(u => u.Stocks.Add(It.IsAny<Stock>()), Times.Never);
        }

        [Fact]
        public async Task GetOrCreateAsync_ShouldCreateNewStockIfNotExists()
        {
            var productId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();
            var zoneId = Guid.NewGuid();

            _unitOfWorkMock.Setup(u => u.Stocks.GetByProductAndWarehouseAsync(
                productId, warehouseId, zoneId, null))
                .ReturnsAsync((Stock)null);

            var result = await _service.GetOrCreateAsync(productId, warehouseId, zoneId, null);

            result.ProductId.Should().Be(productId);
            result.WarehouseId.Should().Be(warehouseId);
            result.WarehouseZoneId.Should().Be(zoneId);

            _unitOfWorkMock.Verify(u => u.Stocks.Add(result), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region IncreaseStockAsync Tests

        [Fact]
        public async Task IncreaseStockAsync_ShouldIncreaseStockQuantity()
        {
            var stock = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 5m);

            var stockServiceMock = new Mock<StockService>(_unitOfWorkMock.Object, _clockMock.Object) { CallBase = true };
            stockServiceMock.Setup(s => s.GetOrCreateAsync(stock.ProductId, stock.WarehouseId, stock.WarehouseZoneId, null))
                .ReturnsAsync(stock);

            await stockServiceMock.Object.IncreaseStockAsync(stock.ProductId, stock.WarehouseId, stock.WarehouseZoneId, 10, null);

            stock.QuantityTotal.Should().Be(15m);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task IncreaseStockAsync_ShouldThrow_WhenQuantityIsZeroOrNegative()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.IncreaseStockAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, null));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.IncreaseStockAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -5, null));
        }

        #endregion

        #region DecreaseStockAsync Tests

        [Fact]
        public async Task DecreaseStockAsync_ShouldDecreaseStockQuantity()
        {
            var stock = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 20m);

            var stockServiceMock = new Mock<StockService>(_unitOfWorkMock.Object, _clockMock.Object) { CallBase = true };
            stockServiceMock.Setup(s => s.GetOrCreateAsync(stock.ProductId, stock.WarehouseId, stock.WarehouseZoneId, null))
                .ReturnsAsync(stock);

            await stockServiceMock.Object.DecreaseStockAsync(stock.ProductId, stock.WarehouseId, stock.WarehouseZoneId, 5, null);

            stock.QuantityTotal.Should().Be(15m);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DecreaseStockAsync_ShouldThrow_WhenQuantityIsZeroOrNegative()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.DecreaseStockAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, null));
        }

        #endregion

        #region MoveStockAsync Tests

        [Fact]
        public async Task MoveStockAsync_ShouldMoveStockBetweenWarehouses()
        {
            var productId = Guid.NewGuid();
            var sourceWarehouseId = Guid.NewGuid();
            var targetWarehouseId = Guid.NewGuid();
            var sourceZoneId = Guid.NewGuid();
            var targetZoneId = Guid.NewGuid();

            var sourceStock = new Stock(productId, sourceWarehouseId, sourceZoneId, null, 20m);
            var targetStock = new Stock(productId, targetWarehouseId, targetZoneId, null, 5m);

            var stockServiceMock = new Mock<StockService>(_unitOfWorkMock.Object, _clockMock.Object) { CallBase = true };
            stockServiceMock.Setup(s => s.GetOrCreateAsync(productId, sourceWarehouseId, sourceZoneId, null))
                .ReturnsAsync(sourceStock);
            stockServiceMock.Setup(s => s.GetOrCreateAsync(productId, targetWarehouseId, targetZoneId, null))
                .ReturnsAsync(targetStock);

            await stockServiceMock.Object.MoveStockAsync(productId, sourceWarehouseId, sourceZoneId, targetWarehouseId, targetZoneId, 10, null);

            sourceStock.QuantityTotal.Should().Be(10m);
            targetStock.QuantityTotal.Should().Be(15m);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Reservation Tests

        [Fact]
        public async Task ReserveStockAsync_ShouldCreateReservation()
        {
            var stockId = Guid.NewGuid();
            var stock = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 50m);

            _unitOfWorkMock.Setup(u => u.Stocks.FindAsync(stockId)).ReturnsAsync(stock);

            var reservation = await _service.ReserveStockAsync(stockId, 10, "Test", Guid.NewGuid(), null);

            reservation.Quantity.Should().Be(10);
            stock.QuantityReserved.Should().Be(10);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ReleaseReservationAsync_ShouldReleaseReservation()
        {
            var stockId = Guid.NewGuid();
            var stock = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 50m);
            var reservation = stock.CreateReservation(10, "Test", Guid.NewGuid(), null);

            _unitOfWorkMock.Setup(u => u.Stocks.FindAsync(stockId)).ReturnsAsync(stock);

            await _service.ReleaseReservationAsync(stockId, reservation.Id);

            stock.QuantityReserved.Should().Be(0);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CancelReservationAsync_ShouldCancelReservation()
        {
            var stock = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 50m);
            var reservation = stock.CreateReservation(10, "Test", Guid.NewGuid(), null);

            _unitOfWorkMock.Setup(u => u.Stocks.All()).ReturnsAsync(new List<Stock> { stock });

            await _service.CancelReservationAsync(reservation.Id);

            stock.QuantityReserved.Should().Be(0);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ConfirmReservationAsync_ShouldConfirmReservation()
        {
            var stock = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 50m);
            var reservation = stock.CreateReservation(10, "Test", Guid.NewGuid(), null);

            _unitOfWorkMock.Setup(u => u.Stocks.All()).ReturnsAsync(new List<Stock> { stock });

            await _service.ConfirmReservationAsync(reservation.Id);

            stock.QuantityReserved.Should().Be(0);
            stock.QuantityTotal.Should().Be(40);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExpireReservationsAsync_ShouldExpireAllExpiredReservations()
        {
            var now = DateTimeOffset.UtcNow;
            _clockMock.Setup(c => c.UtcNow).Returns(now);

            var reservation1 = new StockReservation(Guid.NewGuid(), 5, "Test", Guid.NewGuid(), now.AddDays(-1));
            var reservation2 = new StockReservation(Guid.NewGuid(), 3, "Test", Guid.NewGuid(), now.AddHours(-1));

            var stock1 = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 10m);
            var stock2 = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 5m);

            _unitOfWorkMock.Setup(u => u.Stocks.GetExpiredReservationsAsync(now))
                .ReturnsAsync(new List<StockReservation> { reservation1, reservation2 });

            _unitOfWorkMock.Setup(u => u.Stocks.FindAsync(reservation1.StockId)).ReturnsAsync(stock1);
            _unitOfWorkMock.Setup(u => u.Stocks.FindAsync(reservation2.StockId)).ReturnsAsync(stock2);

            await _service.ExpireReservationsAsync();

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}