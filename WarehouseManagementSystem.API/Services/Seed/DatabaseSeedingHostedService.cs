using WarehouseManagementSystem.Infrastructure.Persistence;
using WarehouseManagementSystem.Infrastructure.Persistence.Seed;

namespace WarehouseManagementSystem.API.Services.Seed;

public sealed class DatabaseSeedingHostedService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseSeedingHostedService> _logger;

    public DatabaseSeedingHostedService(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseSeedingHostedService> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue<bool>("SeedingEnabled"))
        {
            _logger.LogInformation("Database seeding is disabled.");
            return;
        }

        try
        {
            await SeedAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Database seeding was cancelled because the application is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database seeding failed.");
        }
        finally
        {

        }
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();

        try
        {
            _logger.LogInformation("Database seeding is enabled. Starting master data seed.");
            var masterResult = await DbSeeder.SeedMasterDataAsync(dbContext, cancellationToken: cancellationToken);
            _logger.LogInformation(
                "Master data seed finished. Warehouses: {Warehouses}, Zones: {Zones}, Products: {Products}, ProductBatches: {ProductBatches}, Stocks: {Stocks}, Skipped: {Skipped}",
                masterResult.Warehouses,
                masterResult.WarehouseZones,
                masterResult.Products,
                masterResult.ProductBatches,
                masterResult.Stocks,
                masterResult.Skipped);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Master data seed failed.");
            throw;
        }

        try
        {
            _logger.LogInformation("Starting operational data seed.");
            var operationalResult = await DbSeeder.SeedOperationalDataAsync(dbContext, cancellationToken: cancellationToken);
            _logger.LogInformation(
                "Operational data seed finished. Documents: {Documents}, DocumentItems: {DocumentItems}, DocumentSequences: {DocumentSequences}, Skipped: {Skipped}",
                operationalResult.Documents,
                operationalResult.DocumentItems,
                operationalResult.DocumentSequences,
                operationalResult.Skipped);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operational data seed failed.");
            throw;
        }
    }
}
