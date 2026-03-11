using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Infrastructure.Persistence;
using WarehouseManagementSystem.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WarehouseManagementSystem.Tests.Infrastructure.Repositories;

public class StockRepositoryTests
{
    private WarehouseManagementSystemDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WarehouseManagementSystemDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WarehouseManagementSystemDbContext(options);
    }

    [Fact]
    public async Task GetByProductAndWarehouseAsync_Should_ReturnCorrectStock()
    {
        // Arrange
        var context = CreateDbContext();
        var repo = new StockRepository(context);

        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        var stock = new Stock(productId, warehouseId, zoneId, batchId, 100);

        context.Stocks.Add(stock);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByProductAndWarehouseAsync(productId, warehouseId, zoneId, batchId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(stock.Id);
    }

    [Fact]
    public async Task GetActiveReservationsAsync_Should_ReturnOnlyActiveSortedByCreatedAt()
    {
        // Arrange
        var context = CreateDbContext();
        var repo = new StockRepository(context);

        var stock = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 100);
        context.Stocks.Add(stock);

        var r1 = new StockReservation(stock.Id, 5, "TEST", Guid.NewGuid());
        await Task.Delay(5); // różnica czasu dla sortowania
        var r2 = new StockReservation(stock.Id, 5, "TEST", Guid.NewGuid());

        context.StockReservations.AddRange(r1, r2);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetActiveReservationsAsync(stock.Id);

        // Assert
        result.Should().HaveCount(2);
        result.First().CreatedAt.Should().BeBefore(result.Last().CreatedAt);
    }

    [Fact]
    public async Task GetExpiredReservationsAsync_Should_ReturnOnlyExpired()
    {
        // Arrange
        var context = CreateDbContext();
        var repo = new StockRepository(context);

        var stock = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 100);
        context.Stocks.Add(stock);

        var now = DateTimeOffset.UtcNow;

        var expired = new StockReservation(stock.Id, 5, "TEST", Guid.NewGuid(), now.AddMinutes(-1));
        var active = new StockReservation(stock.Id, 5, "TEST", Guid.NewGuid(), now.AddMinutes(10));

        context.StockReservations.AddRange(expired, active);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetExpiredReservationsAsync(now);

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(expired.Id);
    }

    [Fact]
    public async Task FindReservationsByStockIdAsync_Should_ReturnReservationsForStock()
    {
        // Arrange
        var context = CreateDbContext();
        var repo = new StockRepository(context);

        var stock1 = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 100);
        var stock2 = new Stock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 100);

        context.Stocks.AddRange(stock1, stock2);

        var r1 = new StockReservation(stock1.Id, 5, "TEST", Guid.NewGuid());
        var r2 = new StockReservation(stock1.Id, 10, "TEST", Guid.NewGuid());
        var r3 = new StockReservation(stock2.Id, 5, "TEST", Guid.NewGuid());

        context.StockReservations.AddRange(r1, r2, r3);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.FindReservationsByStockIdAsync(stock1.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().Contain(new[] { r1.Id, r2.Id });
    }
}