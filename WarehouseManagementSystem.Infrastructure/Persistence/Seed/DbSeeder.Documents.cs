using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.AuditDomain;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.Model.Documents;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

namespace WarehouseManagementSystem.Infrastructure.Persistence.Seed;

public static partial class DbSeeder
{
    private sealed record DocumentSeedResult(
        int Documents,
        int DocumentItems,
        IReadOnlyDictionary<(DocumentType Type, int Year, Guid? WarehouseId), int> SequenceCounters);

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
        var auditLogs = new List<AuditLog>(options.SaveDocumentBatchSize);
        var documentsInBatch = 0;
        var documentsLeftAfterCurrent = 0;
        var itemCount = 0;
        DocumentType documentType = default;
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
            var batchStartDocument = generatedDocuments + 1;
            var batchEndDocument = generatedDocuments + documentsInBatch;

            for (var i = 0; i < documentsInBatch; i++)
            {
                var documentOrdinal = generatedDocuments + 1;

                try
                {
                    documentsLeftAfterCurrent = documentTarget - generatedDocuments - 1;
                    itemCount = CalculateDocumentItemCount(
                        random,
                        options.AverageItemsPerDocument,
                        remainingItems,
                        documentsLeftAfterCurrent);

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
                    document = CreateDocument(
                        random,
                        documentType,
                        documentDate,
                        sourceWarehouse,
                        targetWarehouse,
                        sequenceCounters);

                    for (var itemIndex = 0; itemIndex < itemCount; itemIndex++)
                    {
                        try
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
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            throw CreateOperationalSeedException(
                                "Error generating operational document item.",
                                ex,
                                documentOrdinal,
                                documentTarget,
                                generatedItems,
                                remainingItems,
                                documentType,
                                sourceWarehouse,
                                targetWarehouse,
                                itemCount,
                                itemIndex);
                        }
                    }

                    ApplyOperationalStatus(random, document);
                    documents.Add(document);
                    auditLogs.Add(CreateCreateAuditLog(document));

                    generatedDocuments++;
                    generatedItems += itemCount;
                    remainingItems -= itemCount;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    throw CreateOperationalSeedException(
                        "Error generating operational document.",
                        ex,
                        documentOrdinal,
                        documentTarget,
                        generatedItems,
                        remainingItems,
                        documentType,
                        sourceWarehouse,
                        targetWarehouse,
                        itemCount);
                }
            }

            try
            {
                db.Documents.AddRange(documents);
                db.AuditLogs.AddRange(auditLogs);
                await db.SaveChangesAsync(cancellationToken);
                db.ChangeTracker.Clear();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new InvalidOperationException(
                    $"Error saving operational seed batch. " +
                    $"BatchDocuments={documents.Count}, BatchAuditLogs={auditLogs.Count}, " +
                    $"DocumentRange={batchStartDocument}-{batchEndDocument}, " +
                    $"GeneratedDocuments={generatedDocuments}/{documentTarget}, " +
                    $"GeneratedItems={generatedItems}/{options.MovementItemCount}, " +
                    $"RemainingItems={remainingItems}.",
                    ex);
            }
        }

        return new DocumentSeedResult(
            generatedDocuments,
            generatedItems,
            sequenceCounters);
    }

    private static InvalidOperationException CreateOperationalSeedException(
        string message,
        Exception innerException,
        int documentOrdinal,
        int documentTarget,
        int generatedItems,
        int remainingItems,
        DocumentType documentType,
        Warehouse? sourceWarehouse,
        Warehouse? targetWarehouse,
        int itemCount,
        int? itemIndex = null)
    {
        var itemContext = itemIndex.HasValue
            ? $", ItemIndex={itemIndex.Value + 1}/{itemCount}"
            : string.Empty;

        return new InvalidOperationException(
            $"{message} " +
            $"Document={documentOrdinal}/{documentTarget}{itemContext}, " +
            $"DocumentType={documentType}, " +
            $"SourceWarehouseId={sourceWarehouse?.Id}, " +
            $"TargetWarehouseId={targetWarehouse?.Id}, " +
            $"GeneratedItems={generatedItems}, " +
            $"RemainingItems={remainingItems}.",
            innerException);
    }

    private static int CalculateDocumentItemCount(
        Random random,
        int averageItemsPerDocument,
        int remainingItems,
        int documentsLeftAfterCurrent)
    {
        const int minItemsPerDocument = 1;
        var maxItemsPerDocument = averageItemsPerDocument + 2;
        var desiredItemCount = Math.Max(
            minItemsPerDocument,
            averageItemsPerDocument + random.Next(-2, 3));

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
        sequenceCounters[sequenceKey] = ++lastNumber;

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

        // Keep generated PZ documents compatible with both DocumentItem validation and command-service stock logic.
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
        if (roll < 5) { return; }

        if (roll < 8)
        {
            document.Cancel(PickUser(random));
            return;
        }

        document.Confirm(PickUser(random));

        if (roll >= 94 && document.Type == DocumentType.MM)
        {
            document.StartTransfer(DateTimeOffset.UtcNow.AddMinutes(-random.Next(1, 240)));
        }
    }

    private static DocumentType PickDocumentType(Random random, StockSeedIndex availableStockStates)
    {
        if (!availableStockStates.HasAvailableStock)
        { return random.Next(100) < 85 ? DocumentType.PZ : DocumentType.ADJ; }

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
}
