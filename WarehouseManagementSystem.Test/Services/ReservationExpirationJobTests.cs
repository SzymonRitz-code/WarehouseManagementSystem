using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WarehouseManagementSystem.Domain.Services;
using WarehouseManagementSystem.Infrastructure.Services;
using Xunit;

namespace WarehouseManagementSystem.Tests.Services;

public class ReservationExpirationJobTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<IStockReservationService> _reservationServiceMock = new();
    private readonly Mock<ILogger<ReservationExpirationJob>> _loggerMock = new();

    private ReservationExpirationJob CreateJob()
    {
        _scopeFactoryMock
            .Setup(x => x.CreateScope())
            .Returns(_scopeMock.Object);

        _scopeMock
            .Setup(x => x.ServiceProvider)
            .Returns(_serviceProviderMock.Object);

        _serviceProviderMock
            .Setup(x => x.GetRequiredService(typeof(IStockReservationService)))
            .Returns(_reservationServiceMock.Object);

        return new ReservationExpirationJob(
            _scopeFactoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RunAsync_ShouldResolveReservationService_AndExpireReservations()
    {
        // Arrange
        var job = CreateJob();

        // Act
        await job.RunAsync();

        // Assert
        _reservationServiceMock.Verify(
            x => x.ExpireReservationsAsync(),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldCreateServiceScope()
    {
        var job = CreateJob();

        await job.RunAsync();

        _scopeFactoryMock.Verify(x => x.CreateScope(), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldResolveServiceFromProvider()
    {
        var job = CreateJob();

        await job.RunAsync();

        _serviceProviderMock.Verify(
            x => x.GetRequiredService(typeof(IStockReservationService)),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldLogInformation()
    {
        var job = CreateJob();

        await job.RunAsync();

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RunAsync_ShouldNotThrow_WhenReservationServiceThrows()
    {
        var job = CreateJob();

        _reservationServiceMock
            .Setup(x => x.ExpireReservationsAsync())
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var exception = await Record.ExceptionAsync(() => job.RunAsync());

        // Assert
        exception.Should().BeNull(); // nie powinno propagować wyjątku
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
}