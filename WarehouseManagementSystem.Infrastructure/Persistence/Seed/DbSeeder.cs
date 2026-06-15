using Bogus;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.Model.Documents;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Infrastructure.Persistence.Seed;

/// <summary>
/// Generates realistic master data for a medium-sized warehouse operation.
/// The generated base is intended for later large-scale document and movement seeding.
/// </summary>
public static class DbSeeder
{
    private const int DefaultSeed = 42;

    private static readonly UserSnapshot SystemUser = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "AliceSmith@email.com",
        "Alice Smith");

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
        int SaveDocumentBatchSize = 2_000,
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

        var originalAutoDetectChanges = db.ChangeTracker.AutoDetectChangesEnabled;
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            var random = new Random(options.Seed);
            Randomizer.Seed = new Random(options.Seed);
            var faker = new Faker("en");

            ValidateOptions(options);

            var warehouses = GenerateWarehouses(faker, options.WarehouseCount, options.ZonesPerWarehouse);
            await SaveInBatchesAsync(db, warehouses, cancellationToken);

            var zones = warehouses.SelectMany(x => x.Zones).ToList();

            var products = GenerateProducts(random, faker, options.ProductCount);
            await SaveInBatchesAsync(db, products, cancellationToken);

            var batches = GenerateProductBatches(
                random,
                products,
                options.AverageBatchesPerTrackedProduct);
            await SaveInBatchesAsync(db, batches, cancellationToken);

            var stocks = GenerateStocks(
                random,
                products,
                zones,
                batches,
                options.AverageStockRowsPerProduct);
            await SaveInBatchesAsync(db, stocks, cancellationToken);

            return new Result(
                warehouses.Count,
                zones.Count,
                products.Count,
                batches.Count,
                stocks.Count,
                Skipped: false);
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

        if (warehouses.Count == 0 || products.Count == 0)
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
                cancellationToken);

            var sequences = GenerateDocumentSequences(generated.SequenceCounters);
            await SaveInBatchesAsync(db, sequences, cancellationToken);

            return new OperationalResult(
                generated.Documents,
                generated.DocumentItems,
                sequences.Count,
                Skipped: false);
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

    private static List<DocumentSequence> GenerateDocumentSequences(
        IReadOnlyDictionary<(DocumentType Type, int Year, Guid? WarehouseId), int> sequenceCounters)
    {
        return sequenceCounters
            .Select(x => new DocumentSequence
            {
                Id = Guid.NewGuid(),
                Type = x.Key.Type,
                Year = x.Key.Year,
                WarehouseId = x.Key.WarehouseId,
                LastNumber = x.Value
            })
            .ToList();
    }

    private static async Task<DocumentSeedResult> GenerateDocumentsAsync(
        WarehouseManagementSystemDbContext db,
        OperationalOptions options,
        IReadOnlyList<Warehouse> warehouses,
        IReadOnlyList<Product> products,
        IReadOnlyList<ProductBatch> productBatches,
        CancellationToken cancellationToken)
    {
        var random = new Random(options.Seed + 10_000);
        var batchesByProduct = productBatches
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.ToList());
        var zonesByWarehouse = warehouses
            .ToDictionary(x => x.Id, x => x.Zones.ToList());
        var sequenceCounters = new Dictionary<(DocumentType Type, int Year, Guid? WarehouseId), int>();

        var documentTarget = (int)Math.Ceiling(
            options.MovementItemCount / (double)options.AverageItemsPerDocument);
        var remainingItems = options.MovementItemCount;
        var generatedDocuments = 0;
        var generatedItems = 0;

        var startDate = DateTime.UtcNow.Date.AddYears(-2);
        var dateRangeDays = Math.Max(1, (DateTime.UtcNow.Date - startDate).Days);

        while (generatedDocuments < documentTarget)
        {
            var documents = new List<Document>(options.SaveDocumentBatchSize);
            var documentsInBatch = Math.Min(
                options.SaveDocumentBatchSize,
                documentTarget - generatedDocuments);

            for (var i = 0; i < documentsInBatch; i++)
            {
                var documentsLeftIncludingCurrent = documentTarget - generatedDocuments;
                var maxItemsForCurrentDocument = remainingItems - (documentsLeftIncludingCurrent - 1);
                var itemCount = Math.Min(
                    maxItemsForCurrentDocument,
                    Math.Max(1, options.AverageItemsPerDocument + random.Next(-2, 3)));

                var documentType = PickDocumentType(random);
                var sourceWarehouse = PickWarehouseForDocument(random, warehouses);
                var targetWarehouse = documentType == DocumentType.MM
                    ? PickDifferentWarehouse(random, warehouses, sourceWarehouse)
                    : PickWarehouseForDocument(random, warehouses);
                var documentDate = startDate.AddDays(random.Next(dateRangeDays));
                var document = CreateDocument(
                    random,
                    documentType,
                    documentDate,
                    sourceWarehouse,
                    targetWarehouse,
                    sequenceCounters);

                for (var itemIndex = 0; itemIndex < itemCount; itemIndex++)
                {
                    var product = products[random.Next(products.Count)];
                    batchesByProduct.TryGetValue(product.Id, out var batches);
                    var productBatchId = product.RequiresBatch && batches is { Count: > 0 }
                        ? batches[random.Next(batches.Count)].Id
                        : (Guid?)null;

                    var item = CreateDocumentItem(
                        random,
                        documentType,
                        product,
                        productBatchId,
                        zonesByWarehouse[sourceWarehouse.Id],
                        zonesByWarehouse[targetWarehouse.Id]);

                    document.AddItem(item);
                }

                ApplyOperationalStatus(random, document);
                documents.Add(document);

                generatedDocuments++;
                generatedItems += itemCount;
                remainingItems -= itemCount;
            }

            db.Documents.AddRange(documents);
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }

        return new DocumentSeedResult(
            generatedDocuments,
            generatedItems,
            sequenceCounters);
    }

    private static Document CreateDocument(
        Random random,
        DocumentType documentType,
        DateTime documentDate,
        Warehouse sourceWarehouse,
        Warehouse targetWarehouse,
        Dictionary<(DocumentType Type, int Year, Guid? WarehouseId), int> sequenceCounters)
    {
        Guid? sourceWarehouseId = documentType is DocumentType.WZ or DocumentType.MM or DocumentType.ADJ
            ? sourceWarehouse.Id
            : null;
        Guid? targetWarehouseId = documentType is DocumentType.PZ or DocumentType.MM
            ? targetWarehouse.Id
            : null;

        var document = new Document(
            documentDate,
            documentType,
            SystemUser,
            sourceWarehouseId,
            targetWarehouseId,
            BuildDocumentNotes(random, documentType));

        var sequenceWarehouseId = sourceWarehouseId ?? targetWarehouseId;
        var sequenceKey = (documentType, documentDate.Year, sequenceWarehouseId);
        sequenceCounters.TryGetValue(sequenceKey, out var lastNumber);
        lastNumber++;
        sequenceCounters[sequenceKey] = lastNumber;

        var warehouseCode = sequenceWarehouseId?.ToString("N")[..6].ToUpperInvariant() ?? "GLOBAL";
        document.SetNumber($"{documentType}-{documentDate:yyyy}-{warehouseCode}-{lastNumber:0000000}");

        return document;
    }

    private static string BuildDocumentNotes(Random random, DocumentType documentType)
    {
        string[] reasons = documentType switch
        {
            DocumentType.PZ => ["supplier receipt", "cross-dock inbound", "scheduled replenishment", "return to stock"],
            DocumentType.WZ => ["customer shipment", "store replenishment", "marketplace order", "wholesale dispatch"],
            DocumentType.MM => ["inter-warehouse transfer", "zone replenishment", "reserve to picking transfer", "cold-chain relocation"],
            _ => ["cycle count correction", "quality adjustment", "inventory reconciliation", "damaged goods adjustment"]
        };

        return reasons[random.Next(reasons.Length)];
    }

    private static DocumentItem CreateDocumentItem(
        Random random,
        DocumentType documentType,
        Product product,
        Guid? productBatchId,
        IReadOnlyList<WarehouseZone> sourceZones,
        IReadOnlyList<WarehouseZone> targetZones)
    {
        Guid? sourceZoneId = null;
        Guid? targetZoneId = null;

        if (documentType is DocumentType.WZ or DocumentType.MM)
        {
            sourceZoneId = PickZoneForProduct(random, sourceZones, product).Id;
        }

        if (documentType is DocumentType.PZ or DocumentType.MM)
        {
            targetZoneId = PickZoneForProduct(random, targetZones, product).Id;
        }

        if (documentType == DocumentType.ADJ && random.Next(100) < 70)
        {
            sourceZoneId = PickZoneForProduct(random, sourceZones, product).Id;
        }

        return new DocumentItem(
            product.Id,
            GenerateMovementQuantity(random, product.Unit),
            productBatchId,
            sourceZoneId,
            targetZoneId);
    }

    private static void ApplyOperationalStatus(Random random, Document document)
    {
        var roll = random.Next(100);
        if (roll < 5)
        {
            return;
        }

        if (roll < 8)
        {
            document.Cancel(SystemUser);
            return;
        }

        document.Confirm(SystemUser);

        if (roll >= 94 && document.Type == DocumentType.MM)
        {
            document.StartTransfer(SystemUser.Id, DateTimeOffset.UtcNow.AddMinutes(-random.Next(1, 240)));
        }
    }

    private static DocumentType PickDocumentType(Random random)
    {
        return random.Next(100) switch
        {
            < 42 => DocumentType.WZ,
            < 70 => DocumentType.PZ,
            < 92 => DocumentType.MM,
            _ => DocumentType.ADJ
        };
    }

    private static Warehouse PickWarehouseForDocument(Random random, IReadOnlyList<Warehouse> warehouses)
    {
        if (warehouses.Count == 1)
        {
            return warehouses[0];
        }

        return random.Next(100) switch
        {
            < 55 => warehouses[0],
            < 80 => warehouses[Math.Min(1, warehouses.Count - 1)],
            _ => warehouses[random.Next(warehouses.Count)]
        };
    }

    private static Warehouse PickDifferentWarehouse(
        Random random,
        IReadOnlyList<Warehouse> warehouses,
        Warehouse sourceWarehouse)
    {
        if (warehouses.Count == 1)
        {
            return sourceWarehouse;
        }

        Warehouse targetWarehouse;
        do
        {
            targetWarehouse = PickWarehouseForDocument(random, warehouses);
        }
        while (targetWarehouse.Id == sourceWarehouse.Id);

        return targetWarehouse;
    }

    private static WarehouseZone PickZoneForProduct(
        Random random,
        IReadOnlyList<WarehouseZone> zones,
        Product product)
    {
        var eligibleZones = GetEligibleStockZones(zones, product);
        var pickingZones = eligibleZones.Where(x => x.IsPickingZone).ToList();

        if (pickingZones.Count > 0 && random.Next(100) < 35)
        {
            return pickingZones[random.Next(pickingZones.Count)];
        }

        return eligibleZones[random.Next(eligibleZones.Count)];
    }

    private static decimal GenerateMovementQuantity(Random random, UnitOfMeasure unit)
    {
        var quantity = unit switch
        {
            UnitOfMeasure.Pallet => random.Next(1, 12),
            UnitOfMeasure.Box => random.Next(1, 80),
            UnitOfMeasure.Kilogram => (decimal)(random.NextDouble() * 250 + 1),
            UnitOfMeasure.Gram => (decimal)(random.NextDouble() * 15_000 + 100),
            UnitOfMeasure.Liter => (decimal)(random.NextDouble() * 300 + 1),
            UnitOfMeasure.Milliliter => (decimal)(random.NextDouble() * 24_000 + 250),
            UnitOfMeasure.Meter => (decimal)(random.NextDouble() * 400 + 1),
            UnitOfMeasure.SquareMeter => (decimal)(random.NextDouble() * 250 + 1),
            UnitOfMeasure.CubicMeter => (decimal)(random.NextDouble() * 20 + 0.1),
            _ => random.Next(1, 400)
        };

        return Math.Max(0.01m, Math.Round(quantity, 2));
    }


    private static List<Warehouse> GenerateWarehouses(Faker faker, int warehouseCount, int zonesPerWarehouse)
    {
        var warehouseTemplates = new[]
        {
            ("WH-CHI", "Chicago Distribution Center", "United States", "Chicago"),
            ("WH-DAL", "Dallas Regional Warehouse", "United States", "Dallas"),
            ("WH-ATL", "Atlanta South Hub", "United States", "Atlanta"),
            ("WH-PHX", "Phoenix West Fulfillment Center", "United States", "Phoenix"),
            ("WH-NWK", "Newark Import Terminal", "United States", "Newark")
        };

        var zoneTemplates = new (string Code, string Name, TemperatureType Temperature, bool Picking)[]
        {
            ("RECV", "Receiving", TemperatureType.Ambient, false),
            ("QC", "Quality Control", TemperatureType.Ambient, false),
            ("BUF", "Inbound Buffer", TemperatureType.Ambient, false),
            ("RES-A", "Ambient Reserve Storage", TemperatureType.Ambient, false),
            ("RES-C", "Chilled Reserve Storage", TemperatureType.Cold, false),
            ("RES-F", "Frozen Reserve Storage", TemperatureType.Frozen, false),
            ("PICK-A", "Ambient Picking", TemperatureType.Ambient, true),
            ("PICK-C", "Chilled Picking", TemperatureType.Cold, true),
            ("PACK", "Packing", TemperatureType.Ambient, false),
            ("DISP", "Dispatch", TemperatureType.Ambient, false),
            ("RET", "Returns", TemperatureType.Ambient, false),
            ("VAL", "Value Added Services", TemperatureType.Ambient, false),
            ("DAM", "Damaged Goods", TemperatureType.Ambient, false),
            ("CROSS", "Cross-dock", TemperatureType.Ambient, false)
        };

        var warehouses = new List<Warehouse>(warehouseCount);

        for (var i = 0; i < warehouseCount; i++)
        {
            var template = i < warehouseTemplates.Length
                ? warehouseTemplates[i]
                : ($"WH-{i + 1:000}", $"{faker.Address.City()} Fulfillment Center", "United States", faker.Address.City());

            var warehouse = new Warehouse(
                template.Item1,
                template.Item2,
                template.Item3,
                template.Item4,
                faker.Address.StreetAddress(),
                SystemUser);

            foreach (var zone in GenerateZoneDefinitions(zoneTemplates, zonesPerWarehouse))
            {
                warehouse.AddZone(zone.Code, zone.Name, zone.Temperature, zone.Picking);
            }

            warehouses.Add(warehouse);
        }

        return warehouses;
    }

    private static IEnumerable<(string Code, string Name, TemperatureType Temperature, bool Picking)> GenerateZoneDefinitions(
        IReadOnlyList<(string Code, string Name, TemperatureType Temperature, bool Picking)> zoneTemplates,
        int zonesPerWarehouse)
    {
        for (var i = 0; i < zonesPerWarehouse; i++)
        {
            if (i < zoneTemplates.Count)
            {
                yield return zoneTemplates[i];
                continue;
            }

            var extraIndex = i - zoneTemplates.Count + 1;
            yield return (extraIndex % 5) switch
            {
                0 => ($"PICK-X{extraIndex:00}", $"Overflow Picking {extraIndex:00}", TemperatureType.Ambient, true),
                1 => ($"BULK-X{extraIndex:00}", $"Bulk Storage {extraIndex:00}", TemperatureType.Ambient, false),
                2 => ($"CHILL-X{extraIndex:00}", $"Chilled Overflow {extraIndex:00}", TemperatureType.Cold, false),
                3 => ($"FRZ-X{extraIndex:00}", $"Frozen Overflow {extraIndex:00}", TemperatureType.Frozen, false),
                _ => ($"BUFFER-X{extraIndex:00}", $"Operational Buffer {extraIndex:00}", TemperatureType.Ambient, false)
            };
        }
    }

    private static List<Product> GenerateProducts(Random random, Faker faker, int count)
    {
        var categories = ProductCatalog.Categories;
        var products = new List<Product>(count);

        for (var i = 0; i < count; i++)
        {
            var category = PickWeighted(random, categories);
            var template = category.Products[random.Next(category.Products.Length)];
            var brand = category.Brands[random.Next(category.Brands.Length)];
            var pack = category.PackSizes[random.Next(category.PackSizes.Length)];
            var unit = PickWeighted(random, category.Units);
            var requiresBatch = random.Next(100) < category.BatchTrackingPercent;
            var tradeVariant = BuildTradeVariant(random, category);

            var sku = $"{category.Code}-{i + 1:000000}-{faker.Commerce.Ean13()[^4..]}";
            var name = $"{brand} {template} {tradeVariant} {pack}";
            var description = BuildDescription(category, brand, pack, unit, requiresBatch);

            var (weight, volume) = EstimateDimensions(random, unit, pack);

            products.Add(new Product(
                sku,
                name,
                unit,
                requiresBatch,
                SystemUser,
                weight,
                volume,
                description));
        }

        return products;
    }

    private static string BuildTradeVariant(Random random, Category category)
    {
        string[] variants = category.Code switch
        {
            "DAI" => ["plain", "low fat", "lactose free", "organic", "family pack", "protein enriched"],
            "BEV" => ["still", "sparkling", "no sugar", "classic", "vitamin enriched", "multipack"],
            "FOO" => ["classic", "wholegrain", "premium", "family size", "quick cook", "low salt"],
            "FRZ" => ["deep frozen", "family pack", "ready to cook", "premium", "classic", "bulk pack"],
            "MEA" => ["classic", "smoked", "sliced", "premium", "family pack", "high protein"],
            "HOU" => ["fresh scent", "lemon scent", "sensitive", "concentrated", "professional", "eco"],
            "ELC" => ["black", "white", "fast charge", "compact", "pro", "retail box"],
            "OFF" => ["standard", "recycled", "heavy duty", "bulk pack", "premium", "white"],
            "BHP" => ["standard", "heavy duty", "certified", "blue", "yellow", "industrial"],
            "PHA" => ["standard", "forte", "family pack", "sugar free", "mint", "travel pack"],
            _ => ["standard", "premium", "bulk pack", "retail pack"]
        };

        return variants[random.Next(variants.Length)];
    }

    private static string BuildDescription(
        Category category,
        string brand,
        string pack,
        UnitOfMeasure unit,
        bool requiresBatch)
    {
        var batchInfo = requiresBatch
            ? "requires lot and date tracking"
            : "standard SKU identification";

        var handling = category.Code switch
        {
            "FRZ" => "Store in frozen zone.",
            "DAI" or "MEA" => "Store in chilled zone.",
            "PHA" => "Keep in controlled ambient storage.",
            "HOU" => "Keep away from food picking lanes.",
            _ => "Suitable for standard ambient storage."
        };

        return $"{category.Name}; brand {brand}; pack {pack}; unit {unit}; {batchInfo}. {handling}";
    }

    private static List<ProductBatch> GenerateProductBatches(
        Random random,
        IReadOnlyCollection<Product> products,
        int averageBatchesPerProduct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var batches = new List<ProductBatch>(products.Count / 2 * averageBatchesPerProduct);

        foreach (var product in products.Where(x => x.RequiresBatch))
        {
            var batchCount = Math.Max(2, averageBatchesPerProduct + random.Next(-2, 4));

            for (var i = 0; i < batchCount; i++)
            {
                var manufacturedDaysAgo = random.Next(14, 720);
                var manufactured = today.AddDays(-manufacturedDaysAgo);

                var shelfLifeDays = product.Unit switch
                {
                    UnitOfMeasure.Kilogram or UnitOfMeasure.Gram => random.Next(21, 365),
                    UnitOfMeasure.Liter or UnitOfMeasure.Milliliter => random.Next(90, 540),
                    _ => random.Next(120, 900)
                };

                var expiration = manufactured.AddDays(shelfLifeDays);
                if (random.Next(100) < 4)
                {
                    expiration = today.AddDays(-random.Next(1, 45));
                }
                else if (expiration < today.AddDays(7))
                {
                    expiration = today.AddDays(random.Next(7, 240));
                }

                var batchNumber = $"B{today.Year % 100}{random.Next(1, 53):00}-{product.SKU[^6..]}-{i + 1:00}";
                batches.Add(new ProductBatch(product.Id, batchNumber, SystemUser, manufactured, expiration));
            }
        }

        return batches;
    }

    private static List<Stock> GenerateStocks(
        Random random,
        IReadOnlyList<Product> products,
        IReadOnlyList<WarehouseZone> zones,
        IReadOnlyCollection<ProductBatch> batches,
        int averageRowsPerProduct)
    {
        var batchesByProduct = batches
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var result = new List<Stock>(products.Count * averageRowsPerProduct);
        var keys = new HashSet<(Guid ProductId, Guid WarehouseId, Guid ZoneId, Guid? BatchId)>();

        foreach (var product in products)
        {
            batchesByProduct.TryGetValue(product.Id, out var productBatches);
            var candidates = BuildStockCandidates(random, zones, product, productBatches);
            var maxDistinctRows = candidates.Count;
            var rowCount = Math.Min(
                maxDistinctRows,
                Math.Max(1, averageRowsPerProduct + random.Next(-3, 5)));

            for (var i = 0; i < rowCount; i++)
            {
                var candidate = candidates[i];
                var key = (product.Id, candidate.WarehouseId, candidate.Zone.Id, candidate.BatchId);
                if (!keys.Add(key))
                {
                    continue;
                }

                result.Add(new Stock(
                    product.Id,
                    candidate.WarehouseId,
                    candidate.Zone.Id,
                    candidate.BatchId,
                    GenerateStockQuantity(random, product.Unit, candidate.Zone)));
            }
        }

        return result;
    }

    private static List<(Guid WarehouseId, WarehouseZone Zone, Guid? BatchId)> BuildStockCandidates(
        Random random,
        IEnumerable<WarehouseZone> zones,
        Product product,
        IReadOnlyList<ProductBatch>? productBatches)
    {
        var eligibleZones = GetEligibleStockZones(zones, product);
        var batchIds = product.RequiresBatch && productBatches is { Count: > 0 }
            ? productBatches.Select(x => (Guid?)x.Id).ToList()
            : [null];

        var candidates = eligibleZones
            .SelectMany(zone => batchIds.Select(batchId => (zone.WarehouseId, Zone: zone, BatchId: batchId)))
            .ToList();

        for (var i = candidates.Count - 1; i > 0; i--)
        {
            var swapIndex = random.Next(i + 1);
            (candidates[i], candidates[swapIndex]) = (candidates[swapIndex], candidates[i]);
        }

        return candidates;
    }

    private static List<WarehouseZone> GetEligibleStockZones(
        IEnumerable<WarehouseZone> zones,
        Product product)
    {
        var allowedTemperature = GetRequiredTemperature(product);
        var allZones = zones.ToList();
        var candidates = allZones
            .Where(x => x.TemperatureType == allowedTemperature)
            .ToList();

        return candidates.Count == 0 ? allZones : candidates;
    }

    private static TemperatureType GetRequiredTemperature(Product product)
    {
        if (product.Name.Contains("Frozen", StringComparison.OrdinalIgnoreCase))
        {
            return TemperatureType.Frozen;
        }

        if (product.Name.Contains("Yogurt", StringComparison.OrdinalIgnoreCase)
            || product.Name.Contains("Butter", StringComparison.OrdinalIgnoreCase)
            || product.Name.Contains("Cheese", StringComparison.OrdinalIgnoreCase)
            || product.Name.Contains("Ham", StringComparison.OrdinalIgnoreCase)
            || product.Name.Contains("Sausage", StringComparison.OrdinalIgnoreCase))
        {
            return TemperatureType.Cold;
        }

        return TemperatureType.Ambient;
    }

    private static decimal GenerateStockQuantity(Random random, UnitOfMeasure unit, WarehouseZone zone)
    {
        var pickingFactor = zone.IsPickingZone ? 0.22m : 1m;
        var quantity = unit switch
        {
            UnitOfMeasure.Pallet => random.Next(1, 60),
            UnitOfMeasure.Box => random.Next(8, 420),
            UnitOfMeasure.Kilogram => (decimal)(random.NextDouble() * 900 + 20),
            UnitOfMeasure.Gram => (decimal)(random.NextDouble() * 80_000 + 1_000),
            UnitOfMeasure.Liter => (decimal)(random.NextDouble() * 1_200 + 24),
            UnitOfMeasure.Milliliter => (decimal)(random.NextDouble() * 120_000 + 1_000),
            UnitOfMeasure.Meter => (decimal)(random.NextDouble() * 2_000 + 20),
            UnitOfMeasure.SquareMeter => (decimal)(random.NextDouble() * 800 + 10),
            UnitOfMeasure.CubicMeter => (decimal)(random.NextDouble() * 90 + 1),
            _ => random.Next(24, 6_500)
        };

        return Math.Max(1m, Math.Round(quantity * pickingFactor, 2));
    }

    private static (decimal? Weight, decimal? Volume) EstimateDimensions(
        Random random,
        UnitOfMeasure unit,
        string pack)
    {
        var packMultiplier = pack.Contains("pallet", StringComparison.OrdinalIgnoreCase) ? 80m
            : pack.Contains("case", StringComparison.OrdinalIgnoreCase) || pack.Contains("carton", StringComparison.OrdinalIgnoreCase) ? 12m
            : pack.Contains("shrink pack", StringComparison.OrdinalIgnoreCase) ? 6m
            : 1m;

        var weight = unit switch
        {
            UnitOfMeasure.Kilogram => NextDecimal(random, 0.25m, 25m),
            UnitOfMeasure.Gram => NextDecimal(random, 0.05m, 2.5m),
            UnitOfMeasure.Liter => NextDecimal(random, 0.25m, 18m),
            UnitOfMeasure.Milliliter => NextDecimal(random, 0.05m, 1.5m),
            UnitOfMeasure.Pallet => NextDecimal(random, 120m, 850m),
            UnitOfMeasure.Box => NextDecimal(random, 1m, 18m),
            _ => NextDecimal(random, 0.05m, 12m) * packMultiplier
        };

        var volume = unit switch
        {
            UnitOfMeasure.Liter => NextDecimal(random, 0.001m, 0.05m) * packMultiplier,
            UnitOfMeasure.Milliliter => NextDecimal(random, 0.0002m, 0.008m) * packMultiplier,
            UnitOfMeasure.Pallet => NextDecimal(random, 0.7m, 1.6m),
            UnitOfMeasure.Box => NextDecimal(random, 0.01m, 0.18m),
            _ => NextDecimal(random, 0.0005m, 0.08m) * packMultiplier
        };

        return (Math.Round(weight, 3), Math.Round(volume, 4));
    }

    private static decimal NextDecimal(Random random, decimal min, decimal max)
    {
        return min + (decimal)random.NextDouble() * (max - min);
    }

    private static T PickWeighted<T>(Random random, IReadOnlyList<Weighted<T>> weighted)
    {
        var total = weighted.Sum(x => x.Weight);
        var roll = random.Next(total);
        var cursor = 0;

        foreach (var item in weighted)
        {
            cursor += item.Weight;
            if (roll < cursor)
            {
                return item.Value;
            }
        }

        return weighted[^1].Value;
    }

    private static async Task SaveInBatchesAsync<T>(
        WarehouseManagementSystemDbContext db,
        IReadOnlyList<T> entities,
        CancellationToken cancellationToken)
        where T : class
    {
        const int batchSize = 2_000;

        for (var i = 0; i < entities.Count; i += batchSize)
        {
            db.Set<T>().AddRange(entities.Skip(i).Take(batchSize));
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
