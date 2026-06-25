using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Queries;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Domain.ValueObjects;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.Tests.Services.Queries;

/// <summary>
/// Tests for the <see cref="StockQueryService"/> class in the API services, focusing on querying stock information and availability.
/// </summary>
public class StockQueryServiceTests
{
    private const string TestUserName = "Stock Tester";

    /// <summary>
    /// Tests the GetStocksAsync method to ensure it correctly filters stocks based on search criteria, availability, and projects related names.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetStocksAsync_ShouldFilterBySearchAndAvailableOnly_ThenProjectRelatedNames()
    {
        // Arrange
        await using var context = CreateContext();
        var (warehouse, zone, product, batch) = await SeedReferenceDataAsync(context);
        var matchingStock = CreateStock(product.Id, warehouse.Id, zone.Id, batch.Id, 20, reservedQuantity: 5);
        var emptyStock = CreateStock(product.Id, warehouse.Id, zone.Id, null, 0);
        await AddStocksAsync(context, matchingStock, emptyStock);
        var service = CreateService(context);
        var query = new StockListQuery
        {
            Search = "SKU-001",
            WarehouseId = warehouse.Id,
            ZoneId = zone.Id,
            AvailableOnly = true,
            SortBy = "quantityAvailable",
            SortDirection = "desc"
        };

        // Act
        var result = await service.GetStocksAsync(query);

        // Assert
        result.TotalItems.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(matchingStock.Id);
        result.Items[0].ProductSku.Should().Be("SKU-001");
        result.Items[0].ProductName.Should().Be("Packing Tape");
        result.Items[0].WarehouseName.Should().Be("Main Warehouse");
        result.Items[0].ZoneName.Should().Be("Picking");
        result.Items[0].ProductBatchNumber.Should().Be("BATCH-001");
        result.Items[0].QuantityAvailable.Should().Be(15);
    }

    /// <summary>
    /// Tests the GetByProductAndWarehouseAsync method to ensure it returns only the stock entries that match the specified product and warehouse.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetByProductAndWarehouseAsync_ShouldReturnOnlyMatchingWarehouseStock()
    {
        // Arrange
        await using var context = CreateContext();
        var (warehouse, zone, product, _) = await SeedReferenceDataAsync(context);
        var (otherWarehouse, otherZone) = await AddWarehouseAsync(context, "WH02", "Overflow Warehouse");
        var matchingStock = CreateStock(product.Id, warehouse.Id, zone.Id, null, 10);
        var otherWarehouseStock = CreateStock(product.Id, otherWarehouse.Id, otherZone.Id, null, 7);
        await AddStocksAsync(context, matchingStock, otherWarehouseStock);
        var service = CreateService(context);

        // Act
        var result = await service.GetByProductAndWarehouseAsync(product.Id, warehouse.Id);

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be(matchingStock.Id);
    }

    /// <summary>
    /// Tests the GetStockAvailabilityAsync method to ensure it returns the correct availability projection for all stock entries, including total, reserved, and available quantities.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetStockAvailabilityAsync_ShouldReturnAvailabilityProjectionForAllStock()
    {
        // Arrange
        await using var context = CreateContext();
        var (warehouse, zone, product, _) = await SeedReferenceDataAsync(context);
        var stock = CreateStock(product.Id, warehouse.Id, zone.Id, null, 30, reservedQuantity: 12);
        await AddStocksAsync(context, stock);
        var service = CreateService(context);

        // Act
        var result = await service.GetStockAvailabilityAsync();

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be(stock.Id);
        result[0].QuantityTotal.Should().Be(30);
        result[0].QuantityReserved.Should().Be(12);
        result[0].QuantityAvailable.Should().Be(18);
    }

    /// <summary>
    /// Tests the IsAvailableAsync method to ensure it correctly determines stock availability based on required quantity and matching location, returning true for sufficient stock and false for insufficient stock.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task IsAvailableAsync_ShouldRespectRequiredQuantityAndMatchingLocation()
    {
        // Arrange
        await using var context = CreateContext();
        var (warehouse, zone, product, batch) = await SeedReferenceDataAsync(context);
        var stock = CreateStock(product.Id, warehouse.Id, zone.Id, batch.Id, 10, reservedQuantity: 4);
        await AddStocksAsync(context, stock);
        var service = CreateService(context);

        // Act
        var canReserveSix = await service.IsAvailableAsync(product.Id, warehouse.Id, zone.Id, 6, batch.Id);
        var canReserveSeven = await service.IsAvailableAsync(product.Id, warehouse.Id, zone.Id, 7, batch.Id);

        // Assert
        canReserveSix.Should().BeTrue();
        canReserveSeven.Should().BeFalse();
    }

    /// <summary>
    /// Creates a new instance of the WarehouseManagementSystemDbContext using an in-memory database for testing purposes. Each test will have its own isolated database instance to ensure test independence and avoid side effects.
    /// </summary>
    /// <returns>A new instance of WarehouseManagementSystemDbContext.</returns>
    private static WarehouseManagementSystemDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WarehouseManagementSystemDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    /// <summary>
    /// Creates a new instance of the StockQueryService using the provided WarehouseManagementSystemDbContext. This allows for testing the service methods with the in-memory database context.
    /// </summary>
    /// <param name="context">The in-memory database context to be used by the service.</param>
    /// <returns>A new instance of StockQueryService.</returns>
    private static StockQueryService CreateService(WarehouseManagementSystemDbContext context) => new(context);

    /// <summary>
    /// Creates a new Stock entity with the specified product, warehouse, zone, batch, quantity, and reserved quantity. This helper method is used to seed test data for stock-related tests.
    /// </summary>
    /// <param name="productId">The ID of the product associated with the stock.</param>
    /// <param name="warehouseId">The ID of the warehouse where the stock is located.</param>
    /// <param name="zoneId">The ID of the zone within the warehouse where the stock is located.</param>
    /// <param name="batchId">The ID of the batch associated with the stock, if any.</param>
    /// <param name="quantity">The total quantity of the stock.</param>
    /// <param name="reservedQuantity">The quantity of the stock that is reserved.</param>
    /// <returns>A new instance of Stock with the specified properties.</returns>
    private static Stock CreateStock(
        Guid productId,
        Guid warehouseId,
        Guid zoneId,
        Guid? batchId,
        decimal quantity,
        decimal reservedQuantity = 0)
    {
        var stock = new Stock(productId, warehouseId, zoneId, batchId, quantity);

        if (reservedQuantity > 0)
        {
            stock.IncreaseReserved(reservedQuantity);
        }

        return stock;
    }

    /// <summary>
    /// Adds the specified stock entities to the in-memory database context and saves the changes asynchronously. This helper method is used to seed test data for stock-related tests.
    /// </summary>
    /// <param name="context">The in-memory database context to which the stock entities will be added.</param>
    /// <param name="stocks">The stock entities to be added to the context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task AddStocksAsync(WarehouseManagementSystemDbContext context, params Stock[] stocks)
    {
        context.Stocks.AddRange(stocks);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Adds a new warehouse with the specified code and name to the in-memory database context, along with a default zone. This helper method is used to seed test data for warehouse-related tests.
    /// </summary>
    /// <param name="context">The in-memory database context to which the warehouse will be added.</param>
    /// <param name="code">The code of the warehouse.</param>
    /// <param name="name">The name of the warehouse.</param>
    /// <returns>A task representing the asynchronous operation, containing the created warehouse and its default zone.</returns>
    private static async Task<(Warehouse Warehouse, WarehouseZone Zone)> AddWarehouseAsync(
        WarehouseManagementSystemDbContext context,
        string code,
        string name)
    {
        var warehouse = new Warehouse(code, name, "Poland", "Lodz", "Dock 2", CreateUser());
        var zone = warehouse.AddZone("Z01", "Overflow Picking", TemperatureType.Ambient, true);

        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        return (warehouse, zone);
    }

    /// <summary>
    /// Seeds the in-memory database context with reference data, including a warehouse, zone, product, and product batch. This helper method is used to set up the initial state for stock-related tests.
    /// </summary>
    /// <param name="context">The in-memory database context to which the reference data will be added.</param>
    /// <returns>A task representing the asynchronous operation, containing the created warehouse, zone, product, and product batch.</returns>
    private static async Task<(Warehouse Warehouse, WarehouseZone Zone, Product Product, ProductBatch Batch)> SeedReferenceDataAsync(
        WarehouseManagementSystemDbContext context)
    {
        var warehouse = new Warehouse("WH01", "Main Warehouse", "Poland", "Warsaw", "Dock 1", CreateUser());
        var zone = warehouse.AddZone("Z01", "Picking", TemperatureType.Ambient, true);
        var product = new Product("SKU-001", "Packing Tape", UnitOfMeasure.Piece, true, CreateUser());
        var batch = new ProductBatch(product.Id, "BATCH-001", CreateUser(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));

        context.Warehouses.Add(warehouse);
        context.Products.Add(product);
        context.ProductBatches.Add(batch);
        await context.SaveChangesAsync();

        return (warehouse, zone, product, batch);
    }

    /// <summary>
    /// Creates a new UserSnapshot instance with predefined values for testing purposes. This helper method is used to provide a consistent user context for stock-related tests.
    /// </summary>
    /// <returns>A new UserSnapshot instance with predefined values for testing purposes.</returns>
    private static UserSnapshot CreateUser()
    {
        return new(
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        "stock.test@example.com",
        TestUserName);
    }
}
