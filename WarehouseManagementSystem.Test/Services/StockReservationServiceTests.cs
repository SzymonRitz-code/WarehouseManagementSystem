using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Interfaces.Repositories;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.ValueObjects;
using WarehouseManagementSystem.Infrastructure.Services;

namespace WarehouseManagementSystem.Tests.Services
{
    public class StockReservationServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ISystemClock> _clockMock = new();
        private readonly Mock<IUserService> _userServiceMock = new();
        private readonly StockReservationService _service;

        public StockReservationServiceTests()
        {
            _service = new StockReservationService(_unitOfWorkMock.Object, _clockMock.Object);
            _userServiceMock.Setup(s => s.GetUser(It.IsAny<HttpContext>()))
                .Returns(new UserSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Testomir.Testowski@gmail.com", "Testomir"));
        }

        [Fact]
        public async Task ExpireReservationsAsync_ShouldExpireAllExpiredReservationsAndSaveChanges()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;

            var stock1 = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 10m);
            var stock2 = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 5m);

            // rezerwacje w przyszłości (legalne w domenie)
            var reservation1 = stock1.CreateReservation(5, "Test", _userServiceMock.Object.GetUser(default), now.AddMinutes(5));
            var reservation2 = stock2.CreateReservation(3, "Test", _userServiceMock.Object.GetUser(default), now.AddMinutes(10));

            // przesuwamy czas do przodu
            var future = now.AddMinutes(20);
            _clockMock.Setup(c => c.UtcNow).Returns(future);

            var expiredReservations = new List<StockReservation> { reservation1, reservation2 };

            var stockRepoMock = new Mock<IStockRepository>();

            _unitOfWorkMock
                .Setup(u => u.Stocks)
                .Returns(stockRepoMock.Object);

            stockRepoMock
                .Setup(r => r.GetExpiredReservationsAsync(future))
                .ReturnsAsync(expiredReservations);

            stockRepoMock
                .Setup(r => r.FindAsync(reservation1.StockId))
                .ReturnsAsync(stock1);

            stockRepoMock
                .Setup(r => r.FindAsync(reservation2.StockId))
                .ReturnsAsync(stock2);

            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            await _service.ExpireReservationsAsync();

            // Assert
            reservation1.Status.Should().Be(ReservationStatus.Expired);
            reservation2.Status.Should().Be(ReservationStatus.Expired);

            stock1.QuantityReserved.Should().Be(0);
            stock2.QuantityReserved.Should().Be(0);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExpireReservationsAsync_ShouldSkipReservationIfStockNotFound()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            _clockMock.Setup(c => c.UtcNow).Returns(now);

            var reservation = new StockReservation(Guid.NewGuid(), 5, "Test", _userServiceMock.Object.GetUser(default), now.AddDays(1));

            _unitOfWorkMock.Setup(u => u.Stocks.GetExpiredReservationsAsync(now))
                .ReturnsAsync(new List<StockReservation> { reservation });

            _unitOfWorkMock.Setup(u => u.Stocks.FindAsync(reservation.StockId))
                .ReturnsAsync((Stock)null); // brak stocku
            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
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