using FluentAssertions;
using Moq;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Infrastructure.Services;

namespace WarehouseManagementSystem.Tests.Services
{
    public class StockReservationServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ISystemClock> _clockMock = new();
        private readonly StockReservationService _service;

        public StockReservationServiceTests()
        {
            _service = new StockReservationService(_unitOfWorkMock.Object, _clockMock.Object);
        }

        [Fact]
        public async Task ExpireReservationsAsync_ShouldExpireAllExpiredReservationsAndSaveChanges()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            _clockMock.Setup(c => c.UtcNow).Returns(now);

            var reservation1 = new StockReservation(Guid.NewGuid(), 5, "Test", Guid.NewGuid(), now.AddDays(-1));
            var reservation2 = new StockReservation(Guid.NewGuid(), 3, "Test", Guid.NewGuid(), now.AddHours(-1));

            var expiredReservations = new List<StockReservation> { reservation1, reservation2 };

            var stock1 = new Mock<Stock>(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 10m) { CallBase = true };
            var stock2 = new Mock<Stock>(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 5m) { CallBase = true };

            _unitOfWorkMock.Setup(u => u.Stocks.GetExpiredReservationsAsync(now))
                .ReturnsAsync(expiredReservations);

            _unitOfWorkMock.Setup(u => u.Stocks.FindAsync(reservation1.StockId))
                .ReturnsAsync(stock1.Object);
            _unitOfWorkMock.Setup(u => u.Stocks.FindAsync(reservation2.StockId))
                .ReturnsAsync(stock2.Object);

            // Act
            await _service.ExpireReservationsAsync();

            // Assert
            stock1.Verify(s => s.ExpireReservation(reservation1.Id), Times.Once);
            stock2.Verify(s => s.ExpireReservation(reservation2.Id), Times.Once);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExpireReservationsAsync_ShouldSkipReservationIfStockNotFound()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            _clockMock.Setup(c => c.UtcNow).Returns(now);

            var reservation = new StockReservation(Guid.NewGuid(), 5, "Test", Guid.NewGuid(), now.AddDays(-1));

            _unitOfWorkMock.Setup(u => u.Stocks.GetExpiredReservationsAsync(now))
                .ReturnsAsync(new List<StockReservation> { reservation });

            _unitOfWorkMock.Setup(u => u.Stocks.FindAsync(reservation.StockId))
                .ReturnsAsync((Stock)null); // brak stocku

            // Act
            var exception = await Record.ExceptionAsync(() => _service.ExpireReservationsAsync());

            // Assert
            exception.Should().BeNull();
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExpireReservationsAsync_ShouldHandleNoExpiredReservations()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            _clockMock.Setup(c => c.UtcNow).Returns(now);

            _unitOfWorkMock.Setup(u => u.Stocks.GetExpiredReservationsAsync(now))
                .ReturnsAsync(new List<StockReservation>());

            // Act
            var exception = await Record.ExceptionAsync(() => _service.ExpireReservationsAsync());

            // Assert
            exception.Should().BeNull();
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExpireReservationsAsync_ShouldNotCallExpireReservation_WhenNoExpiredReservations()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            _clockMock.Setup(c => c.UtcNow).Returns(now);

            _unitOfWorkMock.Setup(u => u.Stocks.GetExpiredReservationsAsync(now))
                .ReturnsAsync(new List<StockReservation>());

            // Act
            await _service.ExpireReservationsAsync();

            // Assert
            _unitOfWorkMock.Verify(u => u.Stocks.FindAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}