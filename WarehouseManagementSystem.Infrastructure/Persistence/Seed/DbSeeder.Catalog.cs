using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.Infrastructure.Persistence.Seed;

public static partial class DbSeeder
{
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
