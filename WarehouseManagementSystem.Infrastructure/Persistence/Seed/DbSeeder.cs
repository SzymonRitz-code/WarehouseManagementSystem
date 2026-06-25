using Bogus;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Model.AuditDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Infrastructure.Persistence.Seed;

/// <summary>
/// Generates realistic master data for a medium-sized warehouse operation.
/// The generated base is intended for later large-scale document and movement seeding.
/// </summary>
public static partial class DbSeeder
{
    #region Constants and Seed Users

    private const int DefaultSeed = 42;
    private const int SaveBatchSize = 2_000;

    private static readonly IReadOnlyList<UserSnapshot> SeederUsers =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "AliceSmith@email.com", "Alice Smith"),
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "michael.johnson@northwind-warehouse.com", "Michael Johnson"),
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "sarah.williams@northwind-warehouse.com", "Sarah Williams"),
        new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "david.brown@northwind-warehouse.com", "David Brown"),
        new(Guid.Parse("55555555-5555-5555-5555-555555555555"), "emily.davis@northwind-warehouse.com", "Emily Davis"),
        new(Guid.Parse("66666666-6666-6666-6666-666666666666"), "james.miller@northwind-warehouse.com", "James Miller"),
        new(Guid.Parse("77777777-7777-7777-7777-777777777777"), "linda.wilson@northwind-warehouse.com", "Linda Wilson"),
        new(Guid.Parse("88888888-8888-8888-8888-888888888888"), "robert.moore@northwind-warehouse.com", "Robert Moore"),
        new(Guid.Parse("99999999-9999-9999-9999-999999999999"), "patricia.taylor@northwind-warehouse.com", "Patricia Taylor"),
        new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "william.anderson@northwind-warehouse.com", "William Anderson"),
        new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "barbara.thomas@northwind-warehouse.com", "Barbara Thomas"),
        new(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "richard.jackson@northwind-warehouse.com", "Richard Jackson"),
        new(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), "elizabeth.white@northwind-warehouse.com", "Elizabeth White"),
        new(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), "thomas.harris@northwind-warehouse.com", "Thomas Harris"),
        new(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), "jennifer.martin@northwind-warehouse.com", "Jennifer Martin"),
        new(Guid.Parse("12121212-1212-1212-1212-121212121212"), "charles.thompson@northwind-warehouse.com", "Charles Thompson"),
        new(Guid.Parse("13131313-1313-1313-1313-131313131313"), "mary.garcia@northwind-warehouse.com", "Mary Garcia"),
        new(Guid.Parse("14141414-1414-1414-1414-141414141414"), "christopher.martinez@northwind-warehouse.com", "Christopher Martinez"),
        new(Guid.Parse("15151515-1515-1515-1515-151515151515"), "nancy.robinson@northwind-warehouse.com", "Nancy Robinson"),
        new(Guid.Parse("16161616-1616-1616-1616-161616161616"), "daniel.clark@northwind-warehouse.com", "Daniel Clark"),
        new(Guid.Parse("17171717-1717-1717-1717-171717171717"), "karen.rodriguez@northwind-warehouse.com", "Karen Rodriguez"),
        new(Guid.Parse("18181818-1818-1818-1818-181818181818"), "mark.lewis@northwind-warehouse.com", "Mark Lewis"),
        new(Guid.Parse("19191919-1919-1919-1919-191919191919"), "susan.lee@northwind-warehouse.com", "Susan Lee"),
        new(Guid.Parse("20202020-2020-2020-2020-202020202020"), "kevin.walker@northwind-warehouse.com", "Kevin Walker")
    ];

    #endregion

    #region Seed Options and Results

    public sealed record Options(
        int ProductCount = 12_000,
        int WarehouseCount = 3,
        int ZonesPerWarehouse = 12,
        int AverageBatchesPerTrackedProduct = 6,
        int AverageStockRowsPerProduct = 8,
        int Seed = DefaultSeed);

    public sealed record Result(
        int Warehouses,
        int WarehouseZones,
        int Products,
        int ProductBatches,
        int Stocks,
        bool Skipped);

    public sealed record OperationalOptions(
        int MovementItemCount = 10_000_000,
        int AverageItemsPerDocument = 5,
        int SaveDocumentBatchSize = 5_000,
        int Seed = DefaultSeed);

    public sealed record OperationalResult(
        int Documents,
        int DocumentItems,
        int DocumentSequences,
        bool Skipped);

    #endregion

    #region Master Data Seeding

    /// <summary>
    /// Seeds master data (warehouses, zones, products, product batches, and stocks) into the database.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="options">The master data options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an error occurs during master data seeding.</exception>
    public static async Task<Result> SeedMasterDataAsync(
        WarehouseManagementSystemDbContext db,
        Options? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new Options();

        if (await HasMasterDataAsync(db, cancellationToken))
        {
            return new Result(0, 0, 0, 0, 0, Skipped: true);
        }

        ValidateOptions(options);

        var originalAutoDetectChanges = db.ChangeTracker.AutoDetectChangesEnabled;
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            var random = new Random(options.Seed);
            Randomizer.Seed = new Random(options.Seed);
            var faker = new Faker("en");

            var warehouses = GenerateWarehouses(random, faker, options.WarehouseCount, options.ZonesPerWarehouse);
            var zones = warehouses.SelectMany(x => x.Zones).ToList();
            await SaveAsync(db, warehouses, cancellationToken);
            await SaveAsync(
                db,
                warehouses.Select(CreateCreateAuditLog).Concat(zones.Select(CreateCreateAuditLog)).ToList(),
                cancellationToken);

            var products = GenerateProducts(random, faker, options.ProductCount);
            await SaveWithAuditAsync(
                db,
                products,
                products.Select(CreateCreateAuditLog).ToList(),
                cancellationToken);

            var batches = GenerateProductBatches(
                random,
                products,
                options.AverageBatchesPerTrackedProduct);
            await SaveWithAuditAsync(
                db,
                batches,
                batches.Select(CreateCreateAuditLog).ToList(),
                cancellationToken);

            var stocks = GenerateStocks(
                random,
                products,
                zones,
                batches,
                options.AverageStockRowsPerProduct);
            await SaveWithAuditAsync(
                db,
                stocks,
                stocks.Select(stock => CreateCreateAuditLog(stock, PickUser(random))).ToList(),
                cancellationToken);

            return new Result(
                warehouses.Count,
                zones.Count,
                products.Count,
                batches.Count,
                stocks.Count,
                Skipped: false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error seeding master data.", ex);
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = originalAutoDetectChanges;
        }
    }

    #endregion

    #region Operational Data Seeding

    /// <summary>
    /// Seeds operational data (documents and document items) into the database.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="options">The operational options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an error occurs during operational data seeding.</exception>
    public static async Task<OperationalResult> SeedOperationalDataAsync(
        WarehouseManagementSystemDbContext db,
        OperationalOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new OperationalOptions();
        ValidateOperationalOptions(options);

        if (await db.Documents.AnyAsync(cancellationToken)
            || await db.DocumentItems.AnyAsync(cancellationToken))
        {
            return new OperationalResult(0, 0, 0, Skipped: true);
        }

        var warehouses = await db.Warehouses
            .Include(x => x.Zones)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var products = await db.Products
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var productBatches = await db.ProductBatches
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var stocks = await db.Stocks
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (warehouses.Count == 0 || products.Count == 0 || stocks.Count == 0)
        {
            throw new InvalidOperationException("Seed master data before operational data.");
        }

        var originalAutoDetectChanges = db.ChangeTracker.AutoDetectChangesEnabled;
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            var generated = await GenerateDocumentsAsync(
                db,
                options,
                warehouses,
                products,
                productBatches,
                stocks,
                cancellationToken);

            var sequences = GenerateDocumentSequences(generated.SequenceCounters);
            await SaveAsync(db, sequences, cancellationToken);

            return new OperationalResult(
                generated.Documents,
                generated.DocumentItems,
                sequences.Count,
                Skipped: false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error seeding operational data.", ex);
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = originalAutoDetectChanges;
        }
    }

    #endregion

    #region Seed Validation

    private static async Task<bool> HasMasterDataAsync(
        WarehouseManagementSystemDbContext db,
        CancellationToken cancellationToken)
    {
        return await db.Warehouses.AnyAsync(cancellationToken)
            || await db.Products.AnyAsync(cancellationToken)
            || await db.ProductBatches.AnyAsync(cancellationToken)
            || await db.Stocks.AnyAsync(cancellationToken);
    }
    /// <summary>
    /// Validates the provided options to ensure they are within acceptable ranges.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an option is out of the acceptable range.</exception>
    private static void ValidateOptions(Options options)
    {
        if (options.ProductCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.ProductCount), "Product count must be greater than zero.");
        }

        if (options.WarehouseCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.WarehouseCount), "Warehouse count must be greater than zero.");
        }

        if (options.ZonesPerWarehouse <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.ZonesPerWarehouse), "Zones per warehouse must be greater than zero.");
        }

        if (options.AverageBatchesPerTrackedProduct <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.AverageBatchesPerTrackedProduct), "Average batches per tracked product must be greater than zero.");
        }

        if (options.AverageStockRowsPerProduct <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.AverageStockRowsPerProduct), "Average stock rows per product must be greater than zero.");
        }
    }
    /// <summary>
    /// Validates the operational options to ensure they are within acceptable ranges.
    /// </summary>
    /// <param name="options">The operational options to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an option is out of the acceptable range.</exception>
    private static void ValidateOperationalOptions(OperationalOptions options)
    {
        if (options.MovementItemCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MovementItemCount), "Movement item count must be greater than zero.");
        }

        if (options.AverageItemsPerDocument <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.AverageItemsPerDocument), "Average items per document must be greater than zero.");
        }

        if (options.SaveDocumentBatchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.SaveDocumentBatchSize), "Save document batch size must be greater than zero.");
        }
    }

    #endregion

    #region Persistence Helpers

    /// <summary>
    /// Saves entities in batches to the database.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="db"></param>
    /// <param name="entities"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static async Task SaveAsync<T>(
        WarehouseManagementSystemDbContext db,
        IReadOnlyList<T> entities,
        CancellationToken cancellationToken)
        where T : class
    {
        for (var i = 0; i < entities.Count; i += SaveBatchSize)
        {
            db.Set<T>().AddRange(entities.Skip(i).Take(SaveBatchSize));
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }
    }
    /// <summary>
    /// Saves entities and their corresponding audit logs in batches to the database.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="db"></param>
    /// <param name="entities"></param>
    /// <param name="auditLogs"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static async Task SaveWithAuditAsync<T>(
        WarehouseManagementSystemDbContext db,
        IReadOnlyList<T> entities,
        IReadOnlyList<AuditLog> auditLogs,
        CancellationToken cancellationToken)
        where T : class
    {
        for (var i = 0; i < entities.Count; i += SaveBatchSize)
        {
            db.Set<T>().AddRange(entities.Skip(i).Take(SaveBatchSize));
            db.AuditLogs.AddRange(auditLogs.Skip(i).Take(SaveBatchSize));
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }
    }

    #endregion
}
