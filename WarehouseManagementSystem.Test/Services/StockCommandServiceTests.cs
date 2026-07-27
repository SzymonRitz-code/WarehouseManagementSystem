using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using WarehouseManagementSystem.API.Caching;
using WarehouseManagementSystem.API.Services.Stocks.Command;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.ValueObjects;
using WarehouseManagementSystem.Infrastructure.Services;

namespace WarehouseManagementSystem.Tests.Services
{
    public class StockCommandServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ISystemClock> _clockMock = new();
        private readonly Mock<ICacheInvalidationService> _cacheInvalidation = new();
        private readonly Mock<IUserService> _userServiceMock = new();
        private readonly StockCommandService _service;

        public StockCommandServiceTests()
        {
            _service = new StockCommandService(_unitOfWorkMock.Object, _clockMock.Object, _cacheInvalidation.Object);
            _userServiceMock.Setup(s => s.GetUser(It.IsAny<HttpContext>()))
                .Returns(new UserSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Testomir.Testowski@gmail.com", "Testomir"));
        }

        #region GetOrCreateAsync Tests

        /// <summary>
        /// Verifies that GetOrCreateAsync returns existing stock without creating a new one.
        /// </summary>
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

        /// <summary>
        /// Verifies that GetOrCreateAsync creates a new stock when it does not exist and saves it.
        /// </summary>
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
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region IncreaseStockAsync Tests

        /// <summary>
        /// Verifies that IncreaseStockAsync increases stock quantity by the specified amount.
        /// </summary>
        [Fact]
        public async Task IncreaseStockAsync_ShouldIncreaseStockQuantity()
        {
            var stock = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 5m);

            var serviceMock = new Mock<StockCommandService>(_unitOfWorkMock.Object, _clockMock.Object, _cacheInvalidation.Object) { CallBase = true };
            serviceMock.Setup(s => s.GetOrCreateAsync(stock.ProductId, stock.WarehouseId, stock.WarehouseZoneId, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(stock);

            await serviceMock.Object.IncreaseStockAsync(stock.ProductId, stock.WarehouseId, stock.WarehouseZoneId, 10, null);

            stock.QuantityTotal.Should().Be(15m);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Verifies that IncreaseStockAsync throws ArgumentException when quantity is zero or negative.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task IncreaseStockAsync_ShouldThrow_WhenQuantityIsInvalid(decimal qty)
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.IncreaseStockAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), qty, null));
        }

        #endregion

        #region DecreaseStockAsync Tests

        /// <summary>
        /// Verifies that DecreaseStockAsync decreases stock quantity by the specified amount.
        /// </summary>
        [Fact]
        public async Task DecreaseStockAsync_ShouldDecreaseStockQuantity()
        {
            var stock = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 20m);

            var serviceMock = new Mock<StockCommandService>(_unitOfWorkMock.Object, _clockMock.Object, _cacheInvalidation.Object) { CallBase = true };
            serviceMock.Setup(s => s.GetOrCreateAsync(stock.ProductId, stock.WarehouseId, stock.WarehouseZoneId, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(stock);

            await serviceMock.Object.DecreaseStockAsync(stock.ProductId, stock.WarehouseId, stock.WarehouseZoneId, 5, null);

            stock.QuantityTotal.Should().Be(15m);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Verifies that DecreaseStockAsync throws ArgumentException when quantity is invalid (zero or negative).
        /// </summary>
        [Fact]
        public async Task DecreaseStockAsync_ShouldThrow_WhenQuantityIsInvalid()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.DecreaseStockAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, null));
        }

        #endregion

        #region MoveStockAsync Tests

        /// <summary>
        /// Verifies that MoveStockAsync correctly decreases stock in source warehouse and increases it in target warehouse.
        /// </summary>
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

            var serviceMock = new Mock<StockCommandService>(_unitOfWorkMock.Object, _clockMock.Object, _cacheInvalidation.Object) { CallBase = true };
            serviceMock.Setup(s => s.GetOrCreateAsync(productId, sourceWarehouseId, sourceZoneId, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(sourceStock);
            serviceMock.Setup(s => s.GetOrCreateAsync(productId, targetWarehouseId, targetZoneId, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(targetStock);

            await serviceMock.Object.MoveStockAsync(productId, sourceWarehouseId, sourceZoneId, targetWarehouseId, targetZoneId, 10, null);

            sourceStock.QuantityTotal.Should().Be(10m);
            targetStock.QuantityTotal.Should().Be(15m);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Reservation Tests

        /// <summary>
        /// Verifies that ReserveStockAsync creates a reservation with correct quantity and updates reserved quantity.
        /// </summary>
        [Fact]
        public async Task ReserveStockAsync_ShouldCreateReservation()
        {
            var stockId = Guid.NewGuid();
            var stock = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 50m);

            _unitOfWorkMock.Setup(u => u.Stocks.FindAsync(stockId)).ReturnsAsync(stock);

            var reservation = await _service.ReserveStockAsync(stockId, 10, "Test", _userServiceMock.Object.GetUser(default), null);

            reservation.Quantity.Should().Be(10);
            stock.QuantityReserved.Should().Be(10);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Verifies that ReleaseReservationAsync releases a reservation and decreases reserved quantity.
        /// </summary>
        [Fact]
        public async Task ReleaseReservationAsync_ShouldReleaseReservation()
        {
            var stockId = Guid.NewGuid();
            var stock = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 50m);
            var reservation = stock.CreateReservation(10, "Test", _userServiceMock.Object.GetUser(default), null);

            _unitOfWorkMock.Setup(u => u.Stocks.FindAsync(stockId)).ReturnsAsync(stock);

            await _service.ReleaseReservationAsync(stockId, reservation.Id);

            stock.QuantityReserved.Should().Be(0);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Verifies that CancelReservationAsync cancels a reservation and returns reserved quantity to available stock.
        /// </summary>
        [Fact]
        public async Task CancelReservationAsync_ShouldCancelReservation()
        {
            var stock = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 50m);
            var reservation = stock.CreateReservation(10, "Test", _userServiceMock.Object.GetUser(default), null);

            _unitOfWorkMock.Setup(u => u.Stocks.All()).ReturnsAsync(new List<Stock> { stock });

            await _service.CancelReservationAsync(reservation.Id);

            stock.QuantityReserved.Should().Be(0);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Verifies that ConfirmReservationAsync confirms a reservation, reducing available quantity and releasing reserved quantity.
        /// </summary>
        [Fact]
        public async Task ConfirmReservationAsync_ShouldConfirmReservation()
        {
            var stock = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 50m);
            var reservation = stock.CreateReservation(10, "Test", _userServiceMock.Object.GetUser(default), null);

            _unitOfWorkMock.Setup(u => u.Stocks.All()).ReturnsAsync(new List<Stock> { stock });

            await _service.ConfirmReservationAsync(reservation.Id);

            stock.QuantityReserved.Should().Be(0);
            stock.QuantityTotal.Should().Be(40);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Verifies that ExpireReservationsAsync expires all reservations past their expiration date and releases their quantities.
        /// </summary>
        [Fact]
        public async Task ExpireReservationsAsync_ShouldExpireAllExpiredReservations()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            _clockMock.Setup(c => c.UtcNow).Returns(now);

            // Tworzymy stocki
            var stock1 = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 10m);
            var stock2 = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 5m);

            // Tworzymy rezerwacje **przez stock**, aby trafiały do _reservations
            var reservation1 = stock1.CreateReservation(5, "Test1", _userServiceMock.Object.GetUser(default), now.AddMinutes(1));
            var reservation2 = stock2.CreateReservation(3, "Test2", _userServiceMock.Object.GetUser(default), now.AddMinutes(1));

            // Mock UnitOfWork, aby zwracał faktycznie istniejące rezerwacje
            _unitOfWorkMock.Setup(u => u.Stocks.GetExpiredReservationsAsync(now))
                .ReturnsAsync(new List<StockReservation> { reservation1, reservation2 });

            _unitOfWorkMock.Setup(u => u.Stocks.FindAsync(reservation1.StockId))
                .ReturnsAsync(stock1);
            _unitOfWorkMock.Setup(u => u.Stocks.FindAsync(reservation2.StockId))
                .ReturnsAsync(stock2);

            // Act
            await _service.ExpireReservationsAsync();

            // Assert
            reservation1.Status.Should().Be(ReservationStatus.Expired);
            reservation2.Status.Should().Be(ReservationStatus.Expired);

            stock1.QuantityReserved.Should().Be(0);
            stock2.QuantityReserved.Should().Be(0);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}
