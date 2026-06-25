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

/// <summary>
/// Tests for the <see cref="DocumentQueryService"/> class in the API services, focusing on querying documents, filtering, sorting, and pagination.
/// </summary>
public class DocumentQueryServiceTests
{
    private const string TestUserName = "WMS Tester";

    /// <summary>
    /// Tests the GetDocumentsPageAsync method to ensure it correctly filters, sorts, paginates, and projects document rows based on the provided query parameters.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Tests the GetPendingDocumentsAsync method to ensure it returns only draft documents with their item totals, excluding confirmed documents.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Tests the HasActiveReservationsAsync and GetActiveReservationsAsync methods to ensure they correctly identify active reservations for a document and return the corresponding reservation details.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Creates a new instance of the WarehouseManagementSystemDbContext using an in-memory database for testing purposes.
    /// </summary>
    /// <returns>A new instance of <see cref="WarehouseManagementSystemDbContext"/>.</returns>
    private static WarehouseManagementSystemDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WarehouseManagementSystemDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    /// <summary>
    /// Creates a new instance of the DocumentQueryService using the provided DbContext.
    /// </summary>
    /// <param name="context">The DbContext to be used by the service.</param>
    /// <returns>A new instance of <see cref="DocumentQueryService"/>.</returns>
    private static DocumentQueryService CreateService(WarehouseManagementSystemDbContext context) => new(context);

    /// <summary>
    /// Adds the specified documents to the provided DbContext and saves the changes asynchronously.
    /// </summary>
    /// <param name="context">The DbContext to which the documents will be added.</param>
    /// <param name="documents">The documents to be added.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task AddDocumentsAsync(WarehouseManagementSystemDbContext context, params Document[] documents)
    {
        context.Documents.AddRange(documents);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Adds a document, stock, and stock reservation to the provided DbContext and saves the changes asynchronously.
    /// </summary>
    /// <param name="context">The DbContext to which the entities will be added.</param>
    /// <param name="document">The document to be added.</param>
    /// <param name="stock">The stock to be added.</param>
    /// <param name="reservation">The stock reservation to be added.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Seeds reference data into the provided DbContext, including a warehouse, a warehouse zone, and a product. Returns the created entities for further use in tests.
    /// </summary>
    /// <param name="context">The DbContext to which the reference data will be added.</param>
    /// <returns>A tuple containing the created warehouse, warehouse zone, and product.</returns>
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

    /// <summary>
    /// Creates a confirmed document with the specified parameters, including type, warehouse ID, number, quantity, product ID, source zone ID, and target zone ID. The document is created in draft status first and then confirmed.
    /// </summary>
    /// <param name="type">The type of the document.</param>
    /// <param name="warehouseId">The ID of the warehouse.</param>
    /// <param name="number">The document number.</param>
    /// <param name="quantity">The quantity of the product.</param>
    /// <param name="productId">The ID of the product.</param>
    /// <param name="sourceZoneId">The ID of the source zone.</param>
    /// <param name="targetZoneId">The ID of the target zone.</param>
    /// <returns>The created and confirmed document.</returns>
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

    /// <summary>
    /// Creates a draft document with the specified parameters, including type, warehouse ID, quantity, product ID, source zone ID, and target zone ID. The document is created in draft status.
    /// </summary>
    /// <param name="type">The type of the document.</param>
    /// <param name="warehouseId">The ID of the warehouse.</param>
    /// <param name="quantity">The quantity of the product.</param>
    /// <param name="productId">The ID of the product.</param>
    /// <param name="sourceZoneId">The ID of the source zone.</param>
    /// <param name="targetZoneId">The ID of the target zone.</param>
    /// <returns>The created draft document.</returns>
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

    /// <summary>
    /// Creates a UserSnapshot instance with a predefined ID, email, and name for testing purposes.
    /// </summary>
    /// <returns>The created UserSnapshot instance.</returns>
    private static UserSnapshot CreateUser() => new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "wms.test@example.com",
        TestUserName);
}
