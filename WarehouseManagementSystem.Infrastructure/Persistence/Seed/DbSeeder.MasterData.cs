using Bogus;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Infrastructure.Persistence.Seed;

public static partial class DbSeeder
{
    #region Warehouse Generation

    private static List<Warehouse> GenerateWarehouses(Random random, Faker faker, int warehouseCount, int zonesPerWarehouse)
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

    #endregion

    #region Product Generation

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

    #endregion

    #region Product Batch Generation

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

    #endregion

    #region Stock Generation

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
            var rowCount = Math.Min(
                candidates.Count,
                Math.Max(1, averageRowsPerProduct + random.Next(-3, 5)));

            for (var i = 0; i < rowCount; i++)
            {
                var candidate = candidates[i];
                var key = (product.Id, candidate.WarehouseId, candidate.Zone.Id, candidate.BatchId);
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
        { return TemperatureType.Frozen; }

        if (product.Name.Contains("Yogurt", StringComparison.OrdinalIgnoreCase)
            || product.Name.Contains("Butter", StringComparison.OrdinalIgnoreCase)
            || product.Name.Contains("Cheese", StringComparison.OrdinalIgnoreCase)
            || product.Name.Contains("Ham", StringComparison.OrdinalIgnoreCase)
            || product.Name.Contains("Sausage", StringComparison.OrdinalIgnoreCase))
        { return TemperatureType.Cold; }

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

    #endregion

    #region Dimension and User Helpers

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

        foreach (var item in weighted)
        {
            cursor += item.Weight;
            if (roll < cursor) { return item.Value; }
        }

        return weighted[^1].Value;
    }

    #endregion
}
