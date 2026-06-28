using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WarehouseManagementSystem.Domain.Services;

namespace WarehouseManagementSystem.Infrastructure.Services;

public class ReservationExpirationJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationExpirationJob> _logger;

    public ReservationExpirationJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ReservationExpirationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reservation expiration job started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during reservation expiration job");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        _logger.LogInformation("Reservation expiration job stopped");
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var reservationService =
                scope.ServiceProvider.GetRequiredService<IStockReservationService>();

            await reservationService.ExpireReservationsAsync(cancellationToken);

            _logger.LogInformation("Expired reservations processed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing expired reservations");
        }
    }
}
