using WarehouseManagementSystem.Infrastructure.Persistence;
using WarehouseManagementSystem.Infrastructure.Persistence.Seed;

namespace WarehouseManagementSystem.API.Extensions;

public static class DatabaseSeedingExtensions
{
    public static async Task SeedDatabaseIfEnabledAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("SeedingEnabled"))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WarehouseManagementSystemDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        logger.LogInformation("Database seeding is enabled. Starting master data seed.");
        var masterResult = await DbSeeder.SeedMasterDataAsync(dbContext);
        logger.LogInformation(
            "Master data seed finished. Warehouses: {Warehouses}, Zones: {Zones}, Products: {Products}, ProductBatches: {ProductBatches}, Stocks: {Stocks}, Skipped: {Skipped}",
            masterResult.Warehouses,
            masterResult.WarehouseZones,
            masterResult.Products,
            masterResult.ProductBatches,
            masterResult.Stocks,
            masterResult.Skipped);

        logger.LogInformation("Starting operational data seed.");
        var operationalResult = await DbSeeder.SeedOperationalDataAsync(dbContext);
        logger.LogInformation(
            "Operational data seed finished. Documents: {Documents}, DocumentItems: {DocumentItems}, DocumentSequences: {DocumentSequences}, Skipped: {Skipped}",
            operationalResult.Documents,
            operationalResult.DocumentItems,
            operationalResult.DocumentSequences,
            operationalResult.Skipped);
    }
}
