using Bogus;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.AuditDomain;
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

    private readonly record struct StockSeedKey(
        Guid ProductId,
        Guid WarehouseId,
        Guid WarehouseZoneId,
        Guid? ProductBatchId);

    private sealed class StockSeedState
    {
        public StockSeedState(
            Guid productId,
            Guid warehouseId,
            Guid warehouseZoneId,
            Guid? productBatchId,
            decimal available)
        {
            ProductId = productId;
            WarehouseId = warehouseId;
            WarehouseZoneId = warehouseZoneId;
            ProductBatchId = productBatchId;
            Available = available;
        }

        public Guid ProductId { get; }
        public Guid WarehouseId { get; }
        public Guid WarehouseZoneId { get; }
        public Guid? ProductBatchId { get; }
        public decimal Available { get; private set; }
        public int AvailableListIndex { get; set; } = -1;
        public bool IsAvailable => Available > 0;

        public void Increase(decimal quantity)
        {
            Available += quantity;
        }

        public void Decrease(decimal quantity)
        {
            Available -= quantity;
        }
    }

    private sealed class StockSeedIndex
    {
        private readonly Dictionary<StockSeedKey, StockSeedState> _statesByKey;
        private readonly Dictionary<Guid, List<StockSeedState>> _availableByWarehouse = new();
        private readonly Dictionary<Guid, int> _availableWarehouseIndexes = new();
        private readonly List<Guid> _availableWarehouseIds = new();

        public StockSeedIndex(IEnumerable<Stock> stocks)
        {
            _statesByKey = stocks
                .Select(x => new StockSeedState(
                    x.ProductId,
                    x.WarehouseId,
                    x.WarehouseZoneId,
                    x.ProductBatchId,
                    x.Available))
                .ToDictionary(
                    x => new StockSeedKey(x.ProductId, x.WarehouseId, x.WarehouseZoneId, x.ProductBatchId),
                    x => x);

            foreach (var state in _statesByKey.Values.Where(x => x.IsAvailable))
            {
                AddAvailableState(state);
            }
        }

        public bool HasAvailableStock => _availableWarehouseIds.Count > 0;

        public int GetAvailableStockCount(Guid warehouseId)
        {
            return _availableByWarehouse.TryGetValue(warehouseId, out var states)
                ? states.Count
                : 0;
        }

        public Guid PickWarehouseIdWithAvailableStock(Random random)
        {
            if (_availableWarehouseIds.Count == 0)
            {
                throw new InvalidOperationException("Cannot pick warehouse without available stock.");
            }

            return _availableWarehouseIds[random.Next(_availableWarehouseIds.Count)];
        }

        public StockSeedState PickAvailableStock(Random random, Guid warehouseId)
        {
            if (!_availableByWarehouse.TryGetValue(warehouseId, out var states) || states.Count == 0)
            {
                throw new InvalidOperationException("Cannot generate outbound document item without available stock.");
            }

            return states[random.Next(states.Count)];
        }

        public void Increase(
            Guid productId,
            Guid warehouseId,
            Guid warehouseZoneId,
            Guid? productBatchId,
            decimal quantity)
        {
            var key = new StockSeedKey(productId, warehouseId, warehouseZoneId, productBatchId);
            if (!_statesByKey.TryGetValue(key, out var state))
            {
                state = new StockSeedState(productId, warehouseId, warehouseZoneId, productBatchId, 0);
                _statesByKey.Add(key, state);
            }

            var wasAvailable = state.IsAvailable;
            state.Increase(quantity);

            if (!wasAvailable && state.IsAvailable)
            {
                AddAvailableState(state);
            }
        }

        public void Decrease(StockSeedState state, decimal quantity)
        {
            var wasAvailable = state.IsAvailable;
            state.Decrease(quantity);

            if (wasAvailable && !state.IsAvailable)
            {
                RemoveAvailableState(state);
            }
        }

        private void AddAvailableState(StockSeedState state)
        {
            if (!_availableByWarehouse.TryGetValue(state.WarehouseId, out var states))
            {
                states = new List<StockSeedState>();
                _availableByWarehouse.Add(state.WarehouseId, states);
                _availableWarehouseIndexes[state.WarehouseId] = _availableWarehouseIds.Count;
                _availableWarehouseIds.Add(state.WarehouseId);
            }

            state.AvailableListIndex = states.Count;
            states.Add(state);
        }

        private void RemoveAvailableState(StockSeedState state)
        {
            if (!_availableByWarehouse.TryGetValue(state.WarehouseId, out var states))
            {
                return;
            }

            var index = state.AvailableListIndex;
            if (index < 0 || index >= states.Count)
            {
                return;
            }

            var lastIndex = states.Count - 1;
            var lastState = states[lastIndex];
            states[index] = lastState;
            lastState.AvailableListIndex = index;
            states.RemoveAt(lastIndex);
            state.AvailableListIndex = -1;

            if (states.Count == 0)
            {
                _availableByWarehouse.Remove(state.WarehouseId);
                RemoveAvailableWarehouse(state.WarehouseId);
            }
        }

        private void RemoveAvailableWarehouse(Guid warehouseId)
        {
            if (!_availableWarehouseIndexes.TryGetValue(warehouseId, out var index))
            {
                return;
            }

            var lastIndex = _availableWarehouseIds.Count - 1;
            var lastWarehouseId = _availableWarehouseIds[lastIndex];
            _availableWarehouseIds[index] = lastWarehouseId;
            _availableWarehouseIndexes[lastWarehouseId] = index;
            _availableWarehouseIds.RemoveAt(lastIndex);
            _availableWarehouseIndexes.Remove(warehouseId);
        }
    }

    public static async Task<Result> SeedMasterDataAsync(
        WarehouseManagementSystemDbContext db,
        Options? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new Options();

        // The master-data seed is idempotent: if the database already has warehouses, products,
        // batches, or stocks, we do not add another full dataset.
        if (await HasMasterDataAsync(db, cancellationToken))
        {
            return new Result(0, 0, 0, 0, 0, Skipped: true);
        }

        // For large volumes, disable EF Core automatic change detection.
        // Entities are saved in batches, so explicit SaveChanges calls are enough.
        var originalAutoDetectChanges = db.ChangeTracker.AutoDetectChangesEnabled;
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            // A fixed seed makes the dataset repeatable for the same options.
            var random = new Random(options.Seed);
            Randomizer.Seed = new Random(options.Seed);
            var faker = new Faker("en");

            ValidateOptions(options);

            // Warehouses and zones come first because stock records depend on zones.
            var warehouses = GenerateWarehouses(random, faker, options.WarehouseCount, options.ZonesPerWarehouse);
            var zones = warehouses.SelectMany(x => x.Zones).ToList();
            await SaveAsync(db, warehouses, cancellationToken);
            await SaveAsync(
                db,
                warehouses.Select(CreateCreateAuditLog).Concat(zones.Select(CreateCreateAuditLog)).ToList(),
                cancellationToken);

            // Products are distributed realistically across categories, units, and batch tracking.
            var products = GenerateProducts(random, faker, options.ProductCount);
            await SaveWithAuditAsync(
                db,
                products,
                products.Select(CreateCreateAuditLog).ToList(),
                cancellationToken);

            // Batches are created only for products requiring lot/batch tracking.
            var batches = GenerateProductBatches(
                random,
                products,
                options.AverageBatchesPerTrackedProduct);
            await SaveWithAuditAsync(
                db,
                batches,
                batches.Select(CreateCreateAuditLog).ToList(),
                cancellationToken);

            // Stock records connect product, warehouse, zone, and optional batch.
            // This is the base that later operational documents move through.
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
        }catch(Exception ex)
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

        // Operational documents are also seeded only once to avoid duplicating millions of rows.
        if (await db.Documents.AnyAsync(cancellationToken)
            || await db.DocumentItems.AnyAsync(cancellationToken))
        {
            return new OperationalResult(0, 0, 0, Skipped: true);
        }

        // Operational data depends on existing master data: warehouses, zones, products, and batches.
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
            // Generate documents and items in batches instead of keeping all 10 million items in memory.
            var generated = GenerateDocumentsAsync(
                db,
                options,
                warehouses,
                products,
                productBatches,
                stocks,
                cancellationToken);

            // Document sequences are saved at the end from the counters that were actually used.
            var sequences = GenerateDocumentSequences(generated.SequenceCounters);
            await SaveAsync(db, sequences, cancellationToken);

            return new OperationalResult(
                generated.Documents,
                generated.DocumentItems,
                sequences.Count,
                Skipped: false);
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

    private static DocumentSeedResult GenerateDocumentsAsync(
        WarehouseManagementSystemDbContext db,
        OperationalOptions options,
        IReadOnlyList<Warehouse> warehouses,
        IReadOnlyList<Product> products,
        IReadOnlyList<ProductBatch> productBatches,
        IReadOnlyList<Stock> stocks,
        CancellationToken cancellationToken)
    {
        var random = new Random(options.Seed + 10_000);
        // Dictionaries speed up batch and zone lookup inside the loop that creates millions of items.
        var batchesByProduct = productBatches
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.ToList());
        var productsById = products.ToDictionary(x => x.Id);
        var warehousesById = warehouses.ToDictionary(x => x.Id);
        var zonesByWarehouse = warehouses
            .ToDictionary(x => x.Id, x => x.Zones.ToList());
        var availableStockStates = new StockSeedIndex(stocks);
        var sequenceCounters = new Dictionary<(DocumentType Type, int Year, Guid? WarehouseId), int>();

        // The document count comes from the target item count and average items per document.
        // The final item count remains exact thanks to CalculateDocumentItemCount.
        var documentTarget = (int)Math.Ceiling(
            options.MovementItemCount / (double)options.AverageItemsPerDocument);
        var remainingItems = options.MovementItemCount;
        var generatedDocuments = 0;
        var generatedItems = 0;

        // Document dates are spread across the last two years to look like operational history.
        var startDate = DateTime.UtcNow.Date.AddYears(-2);
        var dateRangeDays = Math.Max(1, (DateTime.UtcNow.Date - startDate).Days);
        var documents = new List<Document>(options.SaveDocumentBatchSize);
        var auditLogs = new List<AuditLog>(options.SaveDocumentBatchSize * (options.AverageItemsPerDocument + 1));
        var documentsInBatch = 0;
        var documentsLeftAfterCurrent = 0;
        var itemCount = 0;
        DocumentType documentType;
        Warehouse sourceWarehouse = null!;
        Warehouse targetWarehouse = null!;
        DateTime documentDate;
        Document document = null!;
        DocumentItem item = null!;

        while (generatedDocuments < documentTarget)
        {
            documents.Clear();
            auditLogs.Clear();
            documentsInBatch = Math.Min(
                options.SaveDocumentBatchSize,
                documentTarget - generatedDocuments);
            for (var i = 0; i < documentsInBatch; i++)
            {
                documentsLeftAfterCurrent = documentTarget - generatedDocuments - 1;
                // Pick a sensible item count for the document while preserving the exact MovementItemCount.
                itemCount = CalculateDocumentItemCount(
                    random,
                    options.AverageItemsPerDocument,
                    remainingItems,
                    documentsLeftAfterCurrent);

                // Document type decides whether warehouse/zone is the source, target, or both sides.
                documentType = PickDocumentType(random, availableStockStates);
                sourceWarehouse = documentType is DocumentType.WZ or DocumentType.MM
                    ? PickWarehouseWithAvailableStock(random, warehousesById, availableStockStates)
                    : PickWarehouseForDocument(random, warehouses);
                targetWarehouse = documentType == DocumentType.MM
                    ? PickDifferentWarehouse(random, warehouses, sourceWarehouse)
                    : sourceWarehouse;
                if (documentType is DocumentType.WZ or DocumentType.MM
                    && availableStockStates.GetAvailableStockCount(sourceWarehouse.Id) < itemCount)
                {
                    documentType = random.Next(100) < 85 ? DocumentType.PZ : DocumentType.ADJ;
                    sourceWarehouse = PickWarehouseForDocument(random, warehouses);
                    targetWarehouse = sourceWarehouse;
                }

                documentDate = startDate.AddDays(random.Next(dateRangeDays));
                // CreateDocument assigns valid document warehouses and a realistic sequence number.
                document = CreateDocument(
                    random,
                    documentType,
                    documentDate,
                    sourceWarehouse,
                    targetWarehouse,
                    sequenceCounters);

                for (var itemIndex = 0; itemIndex < itemCount; itemIndex++)
                {
                    item = documentType is DocumentType.WZ or DocumentType.MM
                        ? CreateOutboundDocumentItem(
                            random,
                            documentType,
                            sourceWarehouse,
                            targetWarehouse,
                            productsById,
                            zonesByWarehouse,
                            availableStockStates)
                        : CreateInboundDocumentItem(
                            random,
                            documentType,
                            sourceWarehouse,
                            products,
                            batchesByProduct,
                            zonesByWarehouse,
                            availableStockStates);

                    document.AddItem(item);
                    auditLogs.Add(CreateMovementAuditLog(document, item));
                }

                ApplyOperationalStatus(random, document);
                documents.Add(document);
                auditLogs.Add(CreateCreateAuditLog(document));

                // Counters track progress and ensure the final item count closes exactly.
                generatedDocuments++;
                generatedItems += itemCount;
                remainingItems -= itemCount;
            }

            // Saving each batch limits memory usage and lets EF Core release tracked entities.
            db.Documents.AddRange(documents);
            db.AuditLogs.AddRange(auditLogs);
            db.SaveChanges();
            db.ChangeTracker.Clear();
        }

        return new DocumentSeedResult(
            generatedDocuments,
            generatedItems,
            sequenceCounters);
    }

    private static int CalculateDocumentItemCount(
        Random random,
        int averageItemsPerDocument,
        int remainingItems,
        int documentsLeftAfterCurrent)
    {
        const int minItemsPerDocument = 1;
        var maxItemsPerDocument = averageItemsPerDocument + 2;
        // Randomize around the average so documents do not all have the same item count.
        var desiredItemCount = Math.Max(
            minItemsPerDocument,
            averageItemsPerDocument + random.Next(-2, 3));

        // Bounds ensure the remaining items can still be distributed across the remaining documents.
        var minCurrentItemCount = Math.Max(
            minItemsPerDocument,
            remainingItems - documentsLeftAfterCurrent * maxItemsPerDocument);
        var maxCurrentItemCount = Math.Min(
            maxItemsPerDocument,
            remainingItems - documentsLeftAfterCurrent * minItemsPerDocument);

        return Math.Clamp(desiredItemCount, minCurrentItemCount, maxCurrentItemCount);
    }

    private static Document CreateDocument(
        Random random,
        DocumentType documentType,
        DateTime documentDate,
        Warehouse sourceWarehouse,
        Warehouse targetWarehouse,
        Dictionary<(DocumentType Type, int Year, Guid? WarehouseId), int> sequenceCounters)
    {
        // Map the document type to the movement sides.
        // PZ receives goods, WZ issues goods, MM transfers goods, ADJ adjusts stock.
        Guid? sourceWarehouseId = documentType is DocumentType.PZ or DocumentType.WZ or DocumentType.MM or DocumentType.ADJ
            ? sourceWarehouse.Id
            : null;
        Guid? targetWarehouseId = documentType is DocumentType.PZ or DocumentType.MM
            ? targetWarehouse.Id
            : null;

        var document = new Document(
            documentDate,
            documentType,
            PickUser(random),
            sourceWarehouseId,
            targetWarehouseId,
            BuildDocumentNotes(random, documentType));

        // Numbering is separate per type, year, and warehouse, similar to a real WMS.
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

        // WZ and MM remove goods from a source zone.
        if (documentType is DocumentType.WZ or DocumentType.MM)
        {
            sourceZoneId = PickZoneForProduct(random, sourceZones, product).Id;
        }

        // PZ and MM put goods into a target zone.
        if (documentType is DocumentType.PZ or DocumentType.MM)
        {
            targetZoneId = PickZoneForProduct(random, targetZones, product).Id;
        }

        // Adjustments often target a specific zone, but some remain global corrections.
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

    private static DocumentItem CreateInboundDocumentItem(
        Random random,
        DocumentType documentType,
        Warehouse warehouse,
        IReadOnlyList<Product> products,
        IReadOnlyDictionary<Guid, List<ProductBatch>> batchesByProduct,
        IReadOnlyDictionary<Guid, List<WarehouseZone>> zonesByWarehouse,
        StockSeedIndex availableStockStates)
    {
        var product = products[random.Next(products.Count)];
        batchesByProduct.TryGetValue(product.Id, out var batches);
        var productBatchId = product.RequiresBatch && batches is { Count: > 0 }
            ? batches[random.Next(batches.Count)].Id
            : (Guid?)null;
        var zone = PickZoneForProduct(random, zonesByWarehouse[warehouse.Id], product);
        var quantity = GenerateMovementQuantity(random, product.Unit);

        // Current DocumentCommandService applies PZ using source warehouse/zone, while
        // DocumentItem's own validation expects a target zone for PZ. Setting both keeps the
        // generated document compatible with both rules until the domain is unified.
        Guid? sourceZoneId = documentType == DocumentType.ADJ && random.Next(100) >= 70
            ? null
            : zone.Id;
        Guid? targetZoneId = documentType == DocumentType.PZ
            ? zone.Id
            : null;

        availableStockStates.Increase(product.Id, warehouse.Id, zone.Id, productBatchId, quantity);

        return new DocumentItem(
            product.Id,
            quantity,
            productBatchId,
            sourceZoneId,
            targetZoneId);
    }

    private static DocumentItem CreateOutboundDocumentItem(
        Random random,
        DocumentType documentType,
        Warehouse sourceWarehouse,
        Warehouse targetWarehouse,
        IReadOnlyDictionary<Guid, Product> productsById,
        IReadOnlyDictionary<Guid, List<WarehouseZone>> zonesByWarehouse,
        StockSeedIndex availableStockStates)
    {
        var stock = availableStockStates.PickAvailableStock(random, sourceWarehouse.Id);
        var product = productsById[stock.ProductId];
        var quantity = GenerateMovementQuantityUpTo(random, product.Unit, stock.Available);
        var targetZoneId = documentType == DocumentType.MM
            ? PickZoneForProduct(random, zonesByWarehouse[targetWarehouse.Id], product).Id
            : (Guid?)null;

        availableStockStates.Decrease(stock, quantity);

        if (documentType == DocumentType.MM && targetZoneId.HasValue)
        {
            availableStockStates.Increase(
                stock.ProductId,
                targetWarehouse.Id,
                targetZoneId.Value,
                stock.ProductBatchId,
                quantity);
        }

        return new DocumentItem(
            stock.ProductId,
            quantity,
            stock.ProductBatchId,
            stock.WarehouseZoneId,
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
            document.Cancel(PickUser(random));
            return;
        }

        document.Confirm(PickUser(random));

        if (roll >= 94 && document.Type == DocumentType.MM)
        {
            document.StartTransfer(PickUser(random).Id, DateTimeOffset.UtcNow.AddMinutes(-random.Next(1, 240)));
        }
    }

    private static DocumentType PickDocumentType(Random random, StockSeedIndex availableStockStates)
    {
        if (!availableStockStates.HasAvailableStock)
        {
            return random.Next(100) < 85 ? DocumentType.PZ : DocumentType.ADJ;
        }

        return random.Next(100) switch
        {
            < 42 => DocumentType.WZ,
            < 70 => DocumentType.PZ,
            < 92 => DocumentType.MM,
            _ => DocumentType.ADJ
        };
    }

    private static Warehouse PickWarehouseWithAvailableStock(
        Random random,
        IReadOnlyDictionary<Guid, Warehouse> warehousesById,
        StockSeedIndex availableStockStates)
    {
        var warehouseId = availableStockStates.PickWarehouseIdWithAvailableStock(random);
        return warehousesById[warehouseId];
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

    private static decimal GenerateMovementQuantityUpTo(Random random, UnitOfMeasure unit, decimal available)
    {
        if (available <= 0)
        {
            throw new InvalidOperationException("Cannot generate movement quantity for empty stock.");
        }

        var desiredQuantity = GenerateMovementQuantity(random, unit);
        var maxQuantity = Math.Max(0.01m, Math.Round(available, 2));
        var quantity = Math.Min(desiredQuantity, maxQuantity);

        if (quantity <= 0.01m)
        {
            return 0.01m;
        }

        return Math.Round(quantity, 2);
    }


    private static List<Warehouse> GenerateWarehouses(Random random, Faker faker, int warehouseCount, int zonesPerWarehouse)
    {
        // Warehouse templates represent a medium operation with a few regional sites.
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
                PickUser(random));

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
            // Categories, units, and batch tracking are weighted, so the assortment is not perfectly flat.
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

            // Names, SKUs, and descriptions are built from domain elements, keeping data varied and coherent.
            products.Add(new Product(
                sku,
                name,
                unit,
                requiresBatch,
                PickUser(random),
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

        // Create batches only where the product requires them; standard SKUs do not inflate batch counts.
        foreach (var product in products.Where(x => x.RequiresBatch))
        {
            var batchCount = Math.Max(2, averageBatchesPerProduct + random.Next(-2, 4));

            for (var i = 0; i < batchCount; i++)
            {
                // Manufacturing and expiration dates are spread in time, with a small expired-batch share.
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
                    var latestExpiredDate = today.AddDays(-1);
                    var earliestValidExpiration = manufactured.AddDays(1);
                    var expiredWindowDays = Math.Max(0, latestExpiredDate.DayNumber - earliestValidExpiration.DayNumber);

                    expiration = expiredWindowDays > 0
                        ? earliestValidExpiration.AddDays(random.Next(1, expiredWindowDays + 1))
                        : manufactured.AddDays(Math.Max(1, shelfLifeDays));
                }
                else if (expiration < today.AddDays(7))
                {
                    expiration = today.AddDays(random.Next(7, 240));
                }

                var batchNumber = $"B{today.Year % 100}{random.Next(1, 53):00}-{product.SKU[^6..]}-{i + 1:00}";
                batches.Add(new ProductBatch(product.Id, batchNumber, PickUser(random), manufactured, expiration));
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
            // Kandydaci to wszystkie sensowne kombinacje: produkt + dopuszczalna strefa + opcjonalna partia.
            var candidates = BuildStockCandidates(random, zones, product, productBatches);
            var maxDistinctRows = candidates.Count;
            var rowCount = Math.Min(
                maxDistinctRows,
                Math.Max(1, averageRowsPerProduct + random.Next(-3, 5)));

            for (var i = 0; i < rowCount; i++)
            {
                var candidate = candidates[i];
                var key = (product.Id, candidate.WarehouseId, candidate.Zone.Id, candidate.BatchId);
                // HashSet keeps stock rows unique for the same product/warehouse/zone/batch combination.
                if (!keys.Add(key)) { continue; }

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
        // Batch-tracked products get separate stock rows per batch; other products use null.
        var batchIds = product.RequiresBatch && productBatches is { Count: > 0 }
            ? productBatches.Select(x => (Guid?)x.Id).ToList()
            : [null];

        var candidates = eligibleZones
            .SelectMany(zone => batchIds.Select(batchId => (zone.WarehouseId, Zone: zone, BatchId: batchId)))
            .ToList();

        for (var i = candidates.Count - 1; i > 0; i--)
        {
            // Shuffle candidates so products do not always land in the same first zones.
            var swapIndex = random.Next(i + 1);
            (candidates[i], candidates[swapIndex]) = (candidates[swapIndex], candidates[i]);
        }

        return candidates;
    }

    private static List<WarehouseZone> GetEligibleStockZones(
        IEnumerable<WarehouseZone> zones,
        Product product)
    {
        // Zone selection respects product temperature: frozen, cold, or ambient.
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

    private static UserSnapshot PickUser(Random random)
    {
        var user = SeederUsers[random.Next(SeederUsers.Count)];
        return new UserSnapshot(user.Id, user.Email, user.Name);
    }

    private static T PickWeighted<T>(Random random, IReadOnlyList<Weighted<T>> weighted)
    {
        var total = weighted.Sum(x => x.Weight);
        var roll = random.Next(total);
        var cursor = 0;

        // Weighted picking gives realistic proportions, for example more FMCG than pharmaceuticals.
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

    private static AuditLog CreateCreateAuditLog(Warehouse warehouse)
    {
        return CreateAuditLog(
            nameof(Warehouse),
            warehouse.Id,
            "Create",
            warehouse.CreatedByUser,
            $"{{\"code\":\"{warehouse.Code}\",\"name\":\"{warehouse.Name}\"}}");
    }

    private static AuditLog CreateCreateAuditLog(WarehouseZone zone)
    {
        return CreateAuditLog(
            nameof(WarehouseZone),
            zone.Id,
            "Create",
            zone.CreatedByUser,
            $"{{\"warehouseId\":\"{zone.WarehouseId}\",\"code\":\"{zone.Code}\",\"name\":\"{zone.Name}\"}}");
    }

    private static AuditLog CreateCreateAuditLog(Product product)
    {
        return CreateAuditLog(
            nameof(Product),
            product.Id,
            "Create",
            product.CreatedByUser,
            $"{{\"sku\":\"{product.SKU}\",\"name\":\"{product.Name}\",\"unit\":\"{product.Unit}\",\"requiresBatch\":{product.RequiresBatch.ToString().ToLowerInvariant()}}}");
    }

    private static AuditLog CreateCreateAuditLog(ProductBatch batch)
    {
        return CreateAuditLog(
            nameof(ProductBatch),
            batch.Id,
            "Create",
            batch.CreatedByUser,
            $"{{\"productId\":\"{batch.ProductId}\",\"batchNumber\":\"{batch.BatchNumber}\"}}");
    }

    private static AuditLog CreateCreateAuditLog(Stock stock, UserSnapshot performedBy)
    {
        return CreateAuditLog(
            nameof(Stock),
            stock.Id,
            "Create",
            performedBy,
            $"{{\"productId\":\"{stock.ProductId}\",\"warehouseId\":\"{stock.WarehouseId}\",\"warehouseZoneId\":\"{stock.WarehouseZoneId}\",\"productBatchId\":\"{stock.ProductBatchId}\",\"quantityTotal\":{stock.QuantityTotal.ToString(CultureInfo.InvariantCulture)}}}");
    }

    private static AuditLog CreateCreateAuditLog(Document document)
    {
        return CreateAuditLog(
            nameof(Document),
            document.Id,
            "Create",
            document.CreatedByUser,
            $"{{\"type\":\"{document.Type}\",\"status\":\"{document.Status}\",\"number\":\"{document.Number}\",\"itemCount\":{document.Items.Count}}}");
    }

    private static AuditLog CreateMovementAuditLog(Document document, DocumentItem item)
    {
        return CreateAuditLog(
            nameof(DocumentItem),
            item.Id,
            "Movement",
            document.CreatedByUser,
            $"{{\"documentId\":\"{document.Id}\",\"documentType\":\"{document.Type}\",\"productId\":\"{item.ProductId}\",\"productBatchId\":\"{item.ProductBatchId}\",\"sourceZoneId\":\"{item.SourceZoneId}\",\"targetZoneId\":\"{item.TargetZoneId}\",\"quantity\":{item.Quantity.ToString(CultureInfo.InvariantCulture)}}}");
    }

    private static AuditLog CreateAuditLog(
        string entityName,
        Guid entityId,
        string operation,
        UserSnapshot performedBy,
        string newValues)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entityName,
            EntityId = entityId,
            Operation = operation,
            OldValues = string.Empty,
            NewValues = newValues,
            PerformedAt = DateTimeOffset.UtcNow,
            IpAddress = "seed",
            PerformedById = performedBy.Id,
            PerformedBy = new UserSnapshot(performedBy.Id, performedBy.Email, performedBy.Name)
        };
    }

    private static async Task SaveAsync<T>(
        WarehouseManagementSystemDbContext db,
        IReadOnlyList<T> entities,
        CancellationToken cancellationToken)
        where T : class
    {
        const int batchSize = 2_000;

        // Generic batch save used for master data and document sequences.
        for (var i = 0; i < entities.Count; i += batchSize)
        {
            db.Set<T>().AddRange(entities.Skip(i).Take(batchSize));
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }
    }

    private static async Task SaveWithAuditAsync<T>(
        WarehouseManagementSystemDbContext db,
        IReadOnlyList<T> entities,
        IReadOnlyList<AuditLog> auditLogs,
        CancellationToken cancellationToken)
        where T : class
    {
        const int batchSize = 2_000;

        for (var i = 0; i < entities.Count; i += batchSize)
        {
            db.Set<T>().AddRange(entities.Skip(i).Take(batchSize));
            db.AuditLogs.AddRange(auditLogs.Skip(i).Take(batchSize));
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

            if (third.HasValue) { units.Add(new Weighted<UnitOfMeasure>(third.Value, thirdWeight)); }

            if (fourth.HasValue) { units.Add(new Weighted<UnitOfMeasure>(fourth.Value, fourthWeight)); }

            return units;
        }
    }
}
