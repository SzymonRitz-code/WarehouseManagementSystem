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
        if (!IsSeedingEnabled())
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
        var seedProfile = GetSeedProfile();
        var masterOptions = GetProfileOptions<DbSeeder.Options>(seedProfile, "MasterData") ?? new DbSeeder.Options();
        var operationalOptions = GetProfileOptions<DbSeeder.OperationalOptions>(seedProfile, "OperationalData")
                                 ?? new DbSeeder.OperationalOptions();

        try
        {
            _logger.LogInformation(
                "Database seeding is enabled. Starting master data seed with profile {SeedProfile}.",
                seedProfile);
            var masterResult = await DbSeeder.SeedMasterDataAsync(
                dbContext,
                masterOptions,
                cancellationToken);
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
            _logger.LogInformation(
                "Starting operational data seed with profile {SeedProfile}. Movement items: {MovementItemCount}.",
                seedProfile,
                operationalOptions.MovementItemCount);
            var operationalResult = await DbSeeder.SeedOperationalDataAsync(
                dbContext,
                operationalOptions,
                cancellationToken);
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

    private bool IsSeedingEnabled()
    {
        return _configuration.GetValue<bool?>("Seeding:Enabled")
               ?? _configuration.GetValue<bool>("SeedingEnabled");
    }

    private string GetSeedProfile()
    {
        return _configuration.GetValue<string>("Seeding:Profile")
               ?? _configuration.GetValue<string>("SeedingProfile")
               ?? "Extreme";
    }

    private TOptions? GetProfileOptions<TOptions>(string profileName, string sectionName)
    {
        return _configuration
                   .GetSection($"Seeding:Profiles:{profileName}:{sectionName}")
                   .Get<TOptions>()
               ?? _configuration
                   .GetSection($"Seeding:{sectionName}")
                   .Get<TOptions>();
    }
}
