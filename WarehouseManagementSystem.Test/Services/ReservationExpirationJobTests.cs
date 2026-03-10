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
            .Setup(x => x.GetService(typeof(IStockReservationService)))
            .Returns(_reservationServiceMock.Object);

        return new ReservationExpirationJob(
            _scopeFactoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RunAsync_ShouldResolveReservationService_AndExpireReservations()
    {
        var job = CreateJob();

        await job.RunAsync();

        _reservationServiceMock.Verify(
            x => x.ExpireReservationsAsync(),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldCreateServiceScope()
    {
        var job = CreateJob();

        await job.RunAsync();

        _scopeFactoryMock.Verify(
            x => x.CreateScope(),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldResolveServiceFromProvider()
    {
        var job = CreateJob();

        await job.RunAsync();

        _serviceProviderMock.Verify(
            x => x.GetService(typeof(IStockReservationService)),
            Times.Once);
    }
}