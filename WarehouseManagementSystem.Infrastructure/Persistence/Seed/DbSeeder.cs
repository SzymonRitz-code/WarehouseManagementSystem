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

    private sealed record DocumentSeedResult(
        int Documents,
        int DocumentItems,
        IReadOnlyDictionary<(DocumentType Type, int Year, Guid? WarehouseId), int> SequenceCounters);

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

    private static async Task<bool> HasMasterDataAsync(
        WarehouseManagementSystemDbContext db,
        CancellationToken cancellationToken)
    {
        return await db.Warehouses.AnyAsync(cancellationToken)
            || await db.Products.AnyAsync(cancellationToken)
            || await db.ProductBatches.AnyAsync(cancellationToken)
            || await db.Stocks.AnyAsync(cancellationToken);
    }

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

        return new DocumentSeedResult(
            generatedDocuments,
            generatedItems,
            sequenceCounters);
    }

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

    private sealed record Weighted<T>(T Value, int Weight);

    private sealed record Category(
        string Code,
        string Name,
        string[] Brands,
        string[] Products,
        string[] PackSizes,
        IReadOnlyList<Weighted<UnitOfMeasure>> Units,
        int BatchTrackingPercent);

    private static class ProductCatalog
    {
        public static readonly IReadOnlyList<Weighted<Category>> Categories =
        [
            new(new Category(
                "DAI",
                "Dairy",
                ["DairyPure", "Green Valley", "Farmstead", "Milko", "Creamline"],
                ["UHT Milk", "Natural Yogurt", "Extra Butter", "Gouda Cheese", "Kefir", "Cottage Cheese"],
                ["200 g", "250 g", "400 g", "500 ml", "1 l", "case 12 pcs"],
                Units(UnitOfMeasure.Piece, 70, UnitOfMeasure.Box, 20, UnitOfMeasure.Liter, 10),
                85),
            13),
            new(new Category(
                "BEV",
                "Beverages",
                ["AquaSpring", "Sunny Orchard", "FreshDrop", "VitalSip", "Northwell"],
                ["Mineral Water", "Orange Juice", "Isotonic Drink", "Fruit Syrup", "Iced Tea"],
                ["330 ml", "500 ml", "1 l", "1.5 l", "shrink pack 6 pcs", "pallet 504 pcs"],
                Units(UnitOfMeasure.Piece, 55, UnitOfMeasure.Box, 25, UnitOfMeasure.Liter, 15, UnitOfMeasure.Pallet, 5),
                55),
            14),
            new(new Category(
                "FOO",
                "Dry Food",
                ["Golden Grain", "PantryPro", "Harvest Table", "KitchenCo", "Daily Meal", "Prime Pantry"],
                ["Fusilli Pasta", "White Rice", "Oat Flakes", "Canned Luncheon Meat", "Tomato Sauce", "All-purpose Seasoning"],
                ["100 g", "250 g", "400 g", "500 g", "1 kg", "case 20 pcs"],
                Units(UnitOfMeasure.Piece, 65, UnitOfMeasure.Box, 25, UnitOfMeasure.Kilogram, 10),
                45),
            16),
            new(new Category(
                "FRZ",
                "Frozen Food",
                ["Frostway", "Arctic Meal", "ColdHarvest", "IceKitchen"],
                ["Frozen Vegetables", "Frozen Dinner Mix", "Frozen Fish Fillet", "Frozen Pizza", "Family Ice Cream"],
                ["300 g", "450 g", "750 g", "1 kg", "case 10 pcs"],
                Units(UnitOfMeasure.Piece, 60, UnitOfMeasure.Box, 25, UnitOfMeasure.Kilogram, 15),
                90),
            8),
            new(new Category(
                "MEA",
                "Meat and Deli",
                ["Smokehouse", "Prime Deli", "Butcher's Choice", "Heritage Meats"],
                ["Canned Ham", "Smoked Sausage", "Frankfurters", "Smoked Bacon", "Salami"],
                ["150 g", "200 g", "250 g", "500 g", "1 kg", "case 8 pcs"],
                Units(UnitOfMeasure.Piece, 65, UnitOfMeasure.Box, 20, UnitOfMeasure.Kilogram, 15),
                90),
            9),
            new(new Category(
                "HOU",
                "Household Chemicals",
                ["CleanMax", "BrightWash", "HomeGuard", "FreshSoft", "CrystalClean"],
                ["Dishwashing Liquid", "Laundry Powder", "Glass Cleaner", "Fabric Softener", "Toilet Gel"],
                ["500 ml", "750 ml", "1 l", "1.5 l", "5 kg", "case 12 pcs"],
                Units(UnitOfMeasure.Piece, 65, UnitOfMeasure.Box, 25, UnitOfMeasure.Liter, 8, UnitOfMeasure.Kilogram, 2),
                30),
            11),
            new(new Category(
                "ELC",
                "Electronics",
                ["Baseus", "Samsung", "Xiaomi", "Logitech", "Green Cell"],
                ["USB-C Cable", "Wall Charger", "Power Bank", "Wireless Headphones", "USB Hub"],
                ["1 pc", "2 pcs", "set", "case 24 pcs"],
                Units(UnitOfMeasure.Piece, 82, UnitOfMeasure.Box, 17, UnitOfMeasure.Pallet, 1),
                4),
            10),
            new(new Category(
                "OFF",
                "Office and Packaging",
                ["Donau", "Esselte", "Grand", "Emerson", "3M"],
                ["A4 Copy Paper", "Ring Binder", "Packing Tape", "Bubble Mailers", "Thermal Labels"],
                ["1 pc", "10 pcs", "100 pcs", "500 sheets", "case 5 pcs", "pallet 240 pcs"],
                Units(UnitOfMeasure.Piece, 55, UnitOfMeasure.Box, 35, UnitOfMeasure.Pallet, 10),
                5),
            9),
            new(new Category(
                "BHP",
                "Safety Supplies",
                ["Uvex", "3M", "Procera", "Delta Plus", "Portwest"],
                ["Nitrile Gloves", "Safety Helmet", "High-visibility Vest", "Safety Glasses", "FFP2 Respirator"],
                ["1 pc", "10 pcs", "20 pcs", "100 pcs", "case 12 packs"],
                Units(UnitOfMeasure.Piece, 70, UnitOfMeasure.Box, 29, UnitOfMeasure.Pallet, 1),
                8),
            6),
            new(new Category(
                "PHA",
                "OTC Pharmaceuticals",
                ["HealthLab", "MediCare", "WellnessCo", "PharmaPlus", "Vital Labs"],
                ["Paracetamol", "Vitamin C", "Magnesium B6", "Antibacterial Gel", "Adhesive Bandages"],
                ["20 tablets", "30 tablets", "50 tablets", "250 ml", "500 ml", "case 24 pcs"],
                Units(UnitOfMeasure.Piece, 75, UnitOfMeasure.Box, 20, UnitOfMeasure.Milliliter, 5),
                95),
            4)
        ];

        private static IReadOnlyList<Weighted<UnitOfMeasure>> Units(
            UnitOfMeasure first,
            int firstWeight,
            UnitOfMeasure second,
            int secondWeight,
            UnitOfMeasure? third = null,
            int thirdWeight = 0,
            UnitOfMeasure? fourth = null,
            int fourthWeight = 0)
        {
            var units = new List<Weighted<UnitOfMeasure>>
            {
                new(first, firstWeight),
                new(second, secondWeight)
            };

            if (third.HasValue)
            {
                units.Add(new Weighted<UnitOfMeasure>(third.Value, thirdWeight));
            }

            if (fourth.HasValue)
            {
                units.Add(new Weighted<UnitOfMeasure>(fourth.Value, fourthWeight));
            }

            return units;
        }
    }
}
