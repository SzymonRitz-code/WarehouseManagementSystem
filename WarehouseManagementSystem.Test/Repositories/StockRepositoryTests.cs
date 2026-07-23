using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Testcontainers.MsSql;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Domain.ValueObjects;
using WarehouseManagementSystem.Infrastructure.Persistence;
using WarehouseManagementSystem.Infrastructure.Persistence.Repositories;

namespace WarehouseManagementSystem.Tests.Infrastructure.Repositories;

/// <summary>
/// Integration tests for <see cref="StockRepository"/> using a real SQL Server instance via Testcontainers.
/// Testcontainers spins up a Docker container with SQL Server, applies EF migrations, and tears it down after the test class.
/// This catches bugs that InMemory provider silently ignores: FK constraints, index violations, concurrent reads, raw SQL.
/// </summary>
[Collection("SqlServer")]
public class StockRepositoryTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private readonly Mock<IUserService> _userServiceMock = new();
    private UserSnapshot _testUser = null!;

    public async Task InitializeAsync()
    {
        _testUser = new UserSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Testomir.Testowski@gmail.com", "Testomir");

        _userServiceMock.Setup(s => s.GetUser(It.IsAny<HttpContext>()))
            .Returns(_testUser);

        await _sqlContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
    }

    private async Task<WarehouseManagementSystemDbContext> CreateDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<WarehouseManagementSystemDbContext>()
            .UseSqlServer(_sqlContainer.GetConnectionString())
            .Options;

        var context = new WarehouseManagementSystemDbContext(options);
        await context.Database.MigrateAsync();
        return context;
    }

    /// <summary>
    /// Seeds the required FK parents (Product, Warehouse, WarehouseZone, optional ProductBatch)
    /// and returns their IDs. SQL Server enforces FK constraints that InMemory silently ignores.
    /// Each call creates fresh UserSnapshot instances to avoid EF change-tracker conflicts
    /// when the same object instance is added to multiple owned entities in one context.
    /// </summary>
    private async Task<(Guid productId, Guid warehouseId, Guid zoneId, Guid? batchId)> SeedParentsAsync(
        WarehouseManagementSystemDbContext context,
        bool withBatch = false)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpper();

        var product = new Product($"SKU-{suffix}", "Test Product", UnitOfMeasure.Piece, withBatch, NewUser());
        var warehouse = new Warehouse(suffix, "Test Warehouse", "PL", "Warsaw", "ul. Testowa 1", NewUser());
        context.Products.Add(product);
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var zone = new WarehouseZone("Z1", "Zone 1", TemperatureType.Ambient, false, warehouse.Id, NewUser());
        context.WarehouseZones.Add(zone);
        await context.SaveChangesAsync();

        Guid? batchId = null;
        if (withBatch)
        {
            var batch = new ProductBatch(product.Id, "BATCH-001", NewUser());
            context.ProductBatches.Add(batch);
            await context.SaveChangesAsync();
            batchId = batch.Id;
        }

        return (product.Id, warehouse.Id, zone.Id, batchId);
    }

    /// <summary>Creates a fresh UserSnapshot instance to avoid EF owned-entity tracking conflicts.</summary>
    private static UserSnapshot NewUser() =>
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Testomir.Testowski@gmail.com", "Testomir");

    /// <summary>
    /// Tests the GetByProductAndWarehouseAsync method of the StockRepository to ensure it returns the correct stock based on product and warehouse identifiers.
    /// </summary>
    [Fact]
    public async Task GetByProductAndWarehouseAsync_Should_ReturnCorrectStock()
    {
        // Arrange
        var context = await CreateDbContextAsync();
        var repo = new StockRepository(context);

        var (productId, warehouseId, zoneId, batchId) = await SeedParentsAsync(context, withBatch: true);
        var stock = new Stock(productId, warehouseId, zoneId, batchId, 100);

        context.Stocks.Add(stock);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByProductAndWarehouseAsync(productId, warehouseId, zoneId, batchId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(stock.Id);
    }

    /// <summary>
    /// Tests the GetActiveReservationsAsync method of the StockRepository to ensure it returns only active reservations sorted by their creation date.
    /// </summary>
    [Fact]
    public async Task GetActiveReservationsAsync_Should_ReturnOnlyActiveSortedByCreatedAt()
    {
        // Arrange
        var context = await CreateDbContextAsync();
        var repo = new StockRepository(context);

        var (productId, warehouseId, zoneId, _) = await SeedParentsAsync(context);
        var stock = new Stock(productId, warehouseId, zoneId, null, 100);
        context.Stocks.Add(stock);
        await context.SaveChangesAsync();

        var r1 = new StockReservation(stock.Id, 5, "TEST", NewUser());
        await Task.Delay(5); // różnica czasu dla sortowania
        var r2 = new StockReservation(stock.Id, 5, "TEST", NewUser());

        context.StockReservations.AddRange(r1, r2);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetActiveReservationsAsync(stock.Id);

        // Assert
        result.Should().HaveCount(2);
        result.First().CreatedAt.Should().BeBefore(result.Last().CreatedAt);
    }

    /// <summary>
    /// Tests the GetExpiredReservationsAsync method of the StockRepository to ensure it returns only expired reservations based on the provided current time.
    /// </summary>
    [Fact]
    public async Task GetExpiredReservationsAsync_Should_ReturnOnlyExpired()
    {
        // Arrange
        var context = await CreateDbContextAsync();
        var repo = new StockRepository(context);

        var (productId, warehouseId, zoneId, _) = await SeedParentsAsync(context);
        var stock = new Stock(productId, warehouseId, zoneId, null, 100);
        context.Stocks.Add(stock);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;

        var expired = new StockReservation(stock.Id, 5, "TEST", NewUser(), now.AddMinutes(1));
        var active = new StockReservation(stock.Id, 5, "TEST", NewUser(), now.AddMinutes(10));

        context.StockReservations.AddRange(expired, active);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetExpiredReservationsAsync(now.AddMinutes(5));

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(expired.Id);
    }

    /// <summary>
    /// Tests the FindReservationsByStockIdAsync method of the StockRepository to ensure it returns all reservations associated with a specific stock ID.
    /// </summary>
    [Fact]
    public async Task FindReservationsByStockIdAsync_Should_ReturnReservationsForStock()
    {
        // Arrange — seed two independent sets of parents using separate contexts to avoid EF tracking conflicts
        Guid productId1, warehouseId1, zoneId1;
        Guid productId2, warehouseId2, zoneId2;

        await using (var seedCtx1 = await CreateDbContextAsync())
        {
            (productId1, warehouseId1, zoneId1, _) = await SeedParentsAsync(seedCtx1);
        }

        await using (var seedCtx2 = await CreateDbContextAsync())
        {
            (productId2, warehouseId2, zoneId2, _) = await SeedParentsAsync(seedCtx2);
        }

        var context = await CreateDbContextAsync();
        var repo = new StockRepository(context);

        var stock1 = new Stock(productId1, warehouseId1, zoneId1, null, 100);
        var stock2 = new Stock(productId2, warehouseId2, zoneId2, null, 100);

        context.Stocks.AddRange(stock1, stock2);
        await context.SaveChangesAsync();

        var r1 = new StockReservation(stock1.Id, 5, "TEST", NewUser());
        var r2 = new StockReservation(stock1.Id, 10, "TEST", NewUser());
        var r3 = new StockReservation(stock2.Id, 5, "TEST", NewUser());

        context.StockReservations.AddRange(r1, r2, r3);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.FindReservationsByStockIdAsync(stock1.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().Contain(new[] { r1.Id, r2.Id });
    }
}