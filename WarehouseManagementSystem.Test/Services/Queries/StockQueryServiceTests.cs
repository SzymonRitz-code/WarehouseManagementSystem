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

public class StockQueryServiceTests
{
    private const string TestUserName = "Stock Tester";

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

    private static WarehouseManagementSystemDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WarehouseManagementSystemDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    private static StockQueryService CreateService(WarehouseManagementSystemDbContext context) => new(context);

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

    private static async Task AddStocksAsync(WarehouseManagementSystemDbContext context, params Stock[] stocks)
    {
        context.Stocks.AddRange(stocks);
        await context.SaveChangesAsync();
    }

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

    private static UserSnapshot CreateUser() => new(
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        "stock.test@example.com",
        TestUserName);
}
