using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.API.DTO;
using WarehouseManagementSystem.API.Services.Queries;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Domain.ValueObjects;
using WarehouseManagementSystem.Infrastructure.Persistence;

namespace WarehouseManagementSystem.Tests.Services.Queries;

public class DocumentQueryServiceTests
{
    private const string TestUserName = "WMS Tester";

    [Fact]
    public async Task GetDocumentsPageAsync_ShouldFilterSortPageAndProjectDocumentRows()
    {
        // Arrange
        await using var context = CreateContext();
        var (warehouse, zone, product) = await SeedReferenceDataAsync(context);
        await AddDocumentsAsync(
            context,
            CreateConfirmedDocument(DocumentType.PZ, warehouse.Id, "PZ/2026/001", 5, product.Id, null, zone.Id),
            CreateConfirmedDocument(DocumentType.WZ, warehouse.Id, "WZ/2026/001", 8, product.Id, zone.Id, null),
            CreateDraftDocument(DocumentType.PZ, warehouse.Id, 3, product.Id, null, zone.Id));
        var service = CreateService(context);
        var query = new DocumentListQuery
        {
            Page = 1,
            PageSize = 1,
            Search = "2026",
            Status = DocumentStatus.Confirmed,
            WarehouseId = warehouse.Id,
            SortBy = "documentNumber",
            SortDirection = "desc"
        };

        // Act
        var result = await service.GetDocumentsPageAsync(query);

        // Assert
        result.TotalItems.Should().Be(2);
        result.Items.Should().ContainSingle();
        result.Items[0].DocumentNumber.Should().Be("WZ/2026/001");
        result.Items[0].SourceWarehouse.Should().Be("Main Warehouse");
        result.Items[0].ItemCount.Should().Be(1);
        result.Items[0].TotalQuantity.Should().Be(8);
    }

    [Fact]
    public async Task GetPendingDocumentsAsync_ShouldReturnOnlyDraftDocumentsWithItemTotals()
    {
        // Arrange
        await using var context = CreateContext();
        var (warehouse, zone, product) = await SeedReferenceDataAsync(context);
        var draft = CreateDraftDocument(DocumentType.PZ, warehouse.Id, 2, product.Id, null, zone.Id);
        draft.AddItem(new DocumentItem(product.Id, 4, null, null, zone.Id));
        await AddDocumentsAsync(
            context,
            draft,
            CreateConfirmedDocument(DocumentType.PZ, warehouse.Id, "PZ/2026/002", 10, product.Id, null, zone.Id));
        var service = CreateService(context);

        // Act
        var result = await service.GetPendingDocumentsAsync();

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be(draft.Id);
        result[0].DocumentNumber.Should().BeNull();
        result[0].Status.Should().Be(DocumentStatus.Draft);
        result[0].ItemCount.Should().Be(2);
        result[0].TotalQuantity.Should().Be(6);
    }

    [Fact]
    public async Task HasActiveReservationsAsync_ShouldMatchDocumentItemStockAndActiveReservation()
    {
        // Arrange
        await using var context = CreateContext();
        var (warehouse, zone, product) = await SeedReferenceDataAsync(context);
        var document = CreateDraftDocument(DocumentType.WZ, warehouse.Id, 5, product.Id, zone.Id, null);
        var stock = new Stock(product.Id, warehouse.Id, zone.Id, null, 20);
        var reservation = stock.CreateReservation(5, document.Id.ToString(), CreateUser());
        await AddDocumentStockAndReservationAsync(context, document, stock, reservation);
        var service = CreateService(context);

        // Act
        var hasActiveReservations = await service.HasActiveReservationsAsync(document.Id);
        var activeReservations = await service.GetActiveReservationsAsync(document.Id);

        // Assert
        hasActiveReservations.Should().BeTrue();
        activeReservations.Should().ContainSingle(r => r.Id == reservation.Id);
    }

    private static WarehouseManagementSystemDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WarehouseManagementSystemDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    private static DocumentQueryService CreateService(WarehouseManagementSystemDbContext context) => new(context);

    private static async Task AddDocumentsAsync(WarehouseManagementSystemDbContext context, params Document[] documents)
    {
        context.Documents.AddRange(documents);
        await context.SaveChangesAsync();
    }

    private static async Task AddDocumentStockAndReservationAsync(
        WarehouseManagementSystemDbContext context,
        Document document,
        Stock stock,
        StockReservation reservation)
    {
        context.Documents.Add(document);
        context.Stocks.Add(stock);
        context.StockReservations.Add(reservation);
        await context.SaveChangesAsync();
    }

    private static async Task<(Warehouse Warehouse, WarehouseZone Zone, Product Product)> SeedReferenceDataAsync(
        WarehouseManagementSystemDbContext context)
    {
        var warehouse = new Warehouse("WH01", "Main Warehouse", "Poland", "Warsaw", "Dock 1", CreateUser());
        var zone = warehouse.AddZone("Z01", "Picking", TemperatureType.Ambient, true);
        var product = new Product("SKU-001", "Packing Tape", UnitOfMeasure.Piece, false, CreateUser());

        context.Warehouses.Add(warehouse);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        return (warehouse, zone, product);
    }

    private static Document CreateConfirmedDocument(
        DocumentType type,
        Guid warehouseId,
        string number,
        decimal quantity,
        Guid productId,
        Guid? sourceZoneId,
        Guid? targetZoneId)
    {
        var document = CreateDraftDocument(type, warehouseId, quantity, productId, sourceZoneId, targetZoneId);
        document.SetNumber(number);
        document.Confirm(CreateUser());
        return document;
    }

    private static Document CreateDraftDocument(
        DocumentType type,
        Guid warehouseId,
        decimal quantity,
        Guid productId,
        Guid? sourceZoneId,
        Guid? targetZoneId)
    {
        var document = new Document(DateTime.UtcNow, type, CreateUser(), warehouseId);
        document.AddItem(new DocumentItem(productId, quantity, null, sourceZoneId, targetZoneId));
        return document;
    }

    private static UserSnapshot CreateUser() => new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "wms.test@example.com",
        TestUserName);
}
