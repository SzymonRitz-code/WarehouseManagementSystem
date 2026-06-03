using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WarehouseManagementSystem.API.Services.AuditLogs;
using WarehouseManagementSystem.API.Services.Documents;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Exceptions;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Interfaces.Repositories;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Services;
using WarehouseManagementSystem.Domain.ValueObjects;
using WarehouseManagementSystem.Infrastructure.Services;

namespace WarehouseManagementSystem.Tests.Integration;

// TODO refactor testów pod nowy przepływ Dokumentów


/// <summary>
/// Tests for DocumentIntegrationTests — application layer orchestration.
/// Domain invariant tests (pure) live in DocumentDomainTests.
/// </summary>
public class DocumentIntegrationTests 
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IStockService> _stock = new();
    private readonly Mock<IDocumentNumberGenerator> _numberGen = new();
    private readonly Mock<ISystemClock> _clock = new();
    private readonly Mock<IDocumentRepository> _docRepo = new();
    private readonly Mock<ILogger<DocumentCommandService>> _logger = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private readonly DocumentCommandService _sut;

    public DocumentIntegrationTests()
    {
        // Single shared repo mock — no per-test setup drift
        _uow.Setup(u => u.Documents).Returns(_docRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _sut = new DocumentCommandService(
            _uow.Object,
            _stock.Object,
            _numberGen.Object,
            _clock.Object,
            _logger.Object, 
            _auditLogService.Object
            );
    }

    // =========================================================
    // Helpers / Builders
    // =========================================================

    /// <summary>
    /// Isolated from any application service — no infrastructure coupling.
    /// </summary>
    private static UserSnapshot AnyUser(string name = "Jan Kowalski", string email = "jan@wms.pl")
        => new(Guid.NewGuid(), email, name);

    private static DocumentItemDraft AnyItemDraft(
        Guid? productId = null,
        Guid? sourceZone = null,
        int quantity = 5)
        => new(productId ?? Guid.NewGuid(), quantity, null, sourceZone ?? Guid.NewGuid(), null);

    /// <summary>
    /// Builds a Draft document with one item — ready for service-level tests.
    /// Does NOT go through the service so Number is guaranteed null.
    /// </summary>
    private static Document DraftDocumentWithItem(
        DocumentType type = DocumentType.PZ,
        Guid? warehouseId = null,
        Guid? targetWareId = null)
    {
        var doc = new Document(
            documentDate: DateTime.UtcNow,
            type: type,
            createdByUser: AnyUser(),
            sourceWarehouseId: warehouseId ?? Guid.NewGuid(),
            targetWarehouseId: targetWareId,
            notes: null);

        var zoneId = Guid.NewGuid();
        var targetZone = targetWareId.HasValue ? Guid.NewGuid() : (Guid?)null;
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 5, null, zoneId, targetZone));

        return doc;
    }

    private void SetupNumberGenerator(string number = "PZ/2024/001")
        => _numberGen
            .Setup(x => x.GenerateAsync(
                It.IsAny<DocumentType>(),
                It.IsAny<Guid?>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(number);

    private void SetupDocumentFound(Document doc)
        => _docRepo.Setup(r => r.FindAsync(doc.Id)).ReturnsAsync(doc);

    // =========================================================
    // CreateDocumentAsync
    // =========================================================

    [Fact]
    public async Task CreateDocument_Throws_WhenItemsEmpty()
    {
        Func<Task> act = () => _sut.CreateDocumentAsync(
            DocumentType.PZ, AnyUser(), Guid.NewGuid(),
            Array.Empty<DocumentItemDraft>(), DateTime.UtcNow);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*at least one item*");
    }

    [Fact]
    public async Task CreateDocument_ReturnsDocumentInDraft_WithNullNumber()
    {
        // Arrange
        var items = new[] { AnyItemDraft() };

        // Act
        var doc = await _sut.CreateDocumentAsync(
            DocumentType.PZ, AnyUser(), Guid.NewGuid(), items, DateTime.UtcNow);

        // Assert — invariant: Number MUST be null after Create
        doc.Status.Should().Be(DocumentStatus.Draft);
        doc.Number.Should().BeNull("Number is assigned only during Confirm, never during Create");
        doc.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateDocument_PersistsDocument_ExactlyOnce()
    {
        var items = new[] { AnyItemDraft() };

        var doc = await _sut.CreateDocumentAsync(
            DocumentType.PZ, AnyUser(), Guid.NewGuid(), items, DateTime.UtcNow);

        _docRepo.Verify(r => r.Add(It.Is<Document>(d => d.Id == doc.Id)), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateDocument_StoresCreatorSnapshot_Correctly()
    {
        // Arrange
        var creator = AnyUser("Anna Nowak", "anna@wms.pl");
        var items = new[] { AnyItemDraft() };

        // Act
        var doc = await _sut.CreateDocumentAsync(
            DocumentType.PZ, creator, Guid.NewGuid(), items, DateTime.UtcNow);

        // Assert — snapshot, not FK
        doc.CreatedByUser.Should().NotBeNull();
        doc.CreatedByUser.Name.Should().Be("Anna Nowak");
        doc.CreatedByUser.Email.Should().Be("anna@wms.pl");
    }

    // =========================================================
    // ConfirmDocumentAsync — Number lifecycle
    // =========================================================

    [Fact]
    public async Task ConfirmDocument_AssignsNumber_FromGenerator()
    {
        // Arrange
        SetupNumberGenerator("PZ/2024/001");
        var doc = DraftDocumentWithItem(DocumentType.PZ);
        SetupDocumentFound(doc);

        // Act
        await _sut.ConfirmDocumentAsync(doc.Id, AnyUser());

        // Assert — core invariant
        doc.Number.Should().Be("PZ/2024/001");
    }

    [Fact]
    public async Task ConfirmDocument_SetsStatusToConfirmed()
    {
        SetupNumberGenerator();
        var doc = DraftDocumentWithItem();
        SetupDocumentFound(doc);

        await _sut.ConfirmDocumentAsync(doc.Id, AnyUser());

        doc.Status.Should().Be(DocumentStatus.Confirmed);
    }

    [Fact]
    public async Task ConfirmDocument_StoresConfirmedBySnapshot()
    {
        SetupNumberGenerator();
        var confirmedBy = AnyUser("Piotr Wiśniewski", "piotr@wms.pl");
        var doc = DraftDocumentWithItem();
        SetupDocumentFound(doc);

        await _sut.ConfirmDocumentAsync(doc.Id, confirmedBy);

        doc.ConfirmedByUser.Should().NotBeNull();
        doc.ConfirmedByUser.Name.Should().Be("Piotr Wiśniewski");
        doc.ConfirmedByUser.Email.Should().Be("piotr@wms.pl");
    }

    [Fact]
    public async Task ConfirmDocument_Throws_WhenDocumentNotFound()
    {
        _docRepo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((Document?)null);

        Func<Task> act = () => _sut.ConfirmDocumentAsync(Guid.NewGuid(), AnyUser());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Document not found*");
    }

    [Fact]
    public async Task ConfirmDocument_Throws_WhenAlreadyConfirmed()
    {
        // Arrange — simulate already-confirmed document (bypassing service)
        SetupNumberGenerator("PZ/2024/001");
        var doc = DraftDocumentWithItem();
        SetupDocumentFound(doc);
        await _sut.ConfirmDocumentAsync(doc.Id, AnyUser()); // first confirm

        // second confirm attempt on same doc
        Func<Task> act = () => _sut.ConfirmDocumentAsync(doc.Id, AnyUser());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Only draft document can be confirmed*");
    }

    // =========================================================
    // ConfirmDocumentAsync — Stock operations
    // =========================================================

    [Fact]
    public async Task ConfirmDocument_IncreasesStock_ForPZ()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();

        SetupNumberGenerator();
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, AnyUser(), warehouseId, null, null);
        doc.AddItem(new DocumentItem(productId, 5, null, zoneId, null));
        SetupDocumentFound(doc);

        await _sut.ConfirmDocumentAsync(doc.Id, AnyUser());

        _stock.Verify(s => s.IncreaseStockAsync(productId, warehouseId, zoneId, 5, null), Times.Once);
    }

    [Fact]
    public async Task ConfirmDocument_DecreasesStock_ForWZ()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();

        SetupNumberGenerator();
        var doc = new Document(DateTime.UtcNow, DocumentType.WZ, AnyUser(), warehouseId, null, null);
        doc.AddItem(new DocumentItem(productId, 10, null, zoneId, null));
        SetupDocumentFound(doc);

        await _sut.ConfirmDocumentAsync(doc.Id, AnyUser());

        _stock.Verify(s => s.DecreaseStockAsync(productId, warehouseId, zoneId, 10, null), Times.Once);
    }

    [Fact]
    public async Task ConfirmDocument_MovesStock_ForMM()
    {
        var productId = Guid.NewGuid();
        var sourceWare = Guid.NewGuid();
        var targetWare = Guid.NewGuid();
        var sourceZone = Guid.NewGuid();
        var targetZone = Guid.NewGuid();

        SetupNumberGenerator();
        var doc = new Document(DateTime.UtcNow, DocumentType.MM, AnyUser(), sourceWare, targetWare, null);
        doc.AddItem(new DocumentItem(productId, 3, null, sourceZone, targetZone));
        SetupDocumentFound(doc);

        await _sut.ConfirmDocumentAsync(doc.Id, AnyUser());

        _stock.Verify(s => s.MoveStockAsync(
            productId, sourceWare, sourceZone, targetWare, targetZone, 3, null), Times.Once);
    }

    [Fact]
    public async Task ConfirmDocument_Throws_ForMM_WhenTargetWarehouseMissing()
    {
        SetupNumberGenerator();
        // MM without targetWarehouseId
        var doc = new Document(DateTime.UtcNow, DocumentType.MM, AnyUser(), Guid.NewGuid(), null, null);
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 1, null, Guid.NewGuid(), Guid.NewGuid()));
        SetupDocumentFound(doc);

        Func<Task> act = () => _sut.ConfirmDocumentAsync(doc.Id, AnyUser());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Target warehouse is required*");
    }

    [Fact]
    public async Task ConfirmDocument_CallsNumberGenerator_WithCorrectParameters()
    {
        var warehouseId = Guid.NewGuid();
        var documentDate = new DateTime(2024, 6, 15);

        SetupNumberGenerator("PZ/2024/001");
        var doc = new Document(documentDate, DocumentType.PZ, AnyUser(), warehouseId, null, null);
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 1, null, Guid.NewGuid(), null));
        SetupDocumentFound(doc);

        await _sut.ConfirmDocumentAsync(doc.Id, AnyUser());

        _numberGen.Verify(g => g.GenerateAsync(
            DocumentType.PZ,
            warehouseId,
            documentDate), Times.Once);
    }

    // =========================================================
    // StartTransferAsync
    // =========================================================

    [Fact]
    public async Task StartTransfer_Throws_WhenDocumentInDraft()
    {
        var doc = DraftDocumentWithItem();
        SetupDocumentFound(doc);

        Func<Task> act = () => _sut.StartTransferAsync(doc.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Only confirmed document can be transferred*");
    }

    [Fact]
    public async Task StartTransfer_SetsStatusToTransfer_WhenConfirmed()
    {
        var doc = DraftDocumentWithItem();
        doc.SetNumber("PZ/2024/001"); // simulate prior confirm (number must be set)
        doc.Confirm(AnyUser());
        SetupDocumentFound(doc);

        var now = DateTimeOffset.UtcNow;
        _clock.Setup(c => c.UtcNow).Returns(now);

        await _sut.StartTransferAsync(doc.Id, Guid.NewGuid());

        doc.Status.Should().Be(DocumentStatus.Transfer);
        doc.TransferStartedAt.Should().Be(now);
    }

    [Fact]
    public async Task StartTransfer_Throws_WhenDocumentCancelled()
    {
        var doc = DraftDocumentWithItem();
        doc.Cancel();
        SetupDocumentFound(doc);

        Func<Task> act = () => _sut.StartTransferAsync(doc.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cancelled document cannot be transferred*");
    }

    [Fact]
    public async Task StartTransfer_SavesChanges_ExactlyOnce()
    {
        var doc = DraftDocumentWithItem();
        doc.SetNumber("PZ/2024/001");
        doc.Confirm(AnyUser());
        SetupDocumentFound(doc);
        _clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        await _sut.StartTransferAsync(doc.Id, Guid.NewGuid());

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================
    // CancelDocumentAsync
    // =========================================================

    [Fact]
    public async Task CancelDocument_SetsStatusToCancelled()
    {
        var doc = DraftDocumentWithItem(DocumentType.WZ);
        SetupDocumentFound(doc);
        _uow.Setup(u => u.Stocks.GetActiveReservationsByDocumentIdAsync(doc.Id))
            .ReturnsAsync(new List<StockReservation>());

        await _sut.CancelDocumentAsync(doc.Id, UserService.GetUser());

        doc.Status.Should().Be(DocumentStatus.Cancelled);
    }

    [Fact]
    public async Task CancelDocument_ReleasesReservations_ForWZ()
    {
        var doc = DraftDocumentWithItem(DocumentType.WZ);
        SetupDocumentFound(doc);

        var reservation = new StockReservation(Guid.NewGuid(), 5, "TEST", Guid.NewGuid());
        _uow.Setup(u => u.Stocks.GetActiveReservationsByDocumentIdAsync(doc.Id))
            .ReturnsAsync(new List<StockReservation> { reservation });

        await _sut.CancelDocumentAsync(doc.Id, UserService.GetUser());

        _stock.Verify(s => s.ReleaseReservationAsync(reservation.StockId, reservation.Id), Times.Once);
        doc.Status.Should().Be(DocumentStatus.Cancelled);
    }

    [Fact]
    public async Task CancelDocument_DoesNotReleaseReservations_WhenNoneExist()
    {
        var doc = DraftDocumentWithItem(DocumentType.WZ);
        SetupDocumentFound(doc);
        _uow.Setup(u => u.Stocks.GetActiveReservationsByDocumentIdAsync(doc.Id))
            .ReturnsAsync(new List<StockReservation>());

        await _sut.CancelDocumentAsync(doc.Id, UserService.GetUser());

        _stock.Verify(s =>
            s.ReleaseReservationAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CancelDocument_Throws_WhenAlreadyCancelled()
    {
        var doc = DraftDocumentWithItem(DocumentType.WZ);
        doc.Cancel(); // first cancel
        SetupDocumentFound(doc);
        _uow.Setup(u => u.Stocks.GetActiveReservationsByDocumentIdAsync(doc.Id))
            .ReturnsAsync(new List<StockReservation>());

        Func<Task> act = () => _sut.CancelDocumentAsync(doc.Id, UserService.GetUser());

        await act.Should().ThrowAsync<DocumentAlreadyCancelledException>()
            .WithMessage($"Document {doc.Id} is already cancelled.");
    }

    [Fact]
    public async Task CancelDocument_Throws_WhenConfirmed()
    {
        var doc = DraftDocumentWithItem();
        doc.SetNumber("PZ/2024/001");
        doc.Confirm(AnyUser());
        SetupDocumentFound(doc);
        _uow.Setup(u => u.Stocks.GetActiveReservationsByDocumentIdAsync(doc.Id))
            .ReturnsAsync(new List<StockReservation>());

        Func<Task> act = () => _sut.CancelDocumentAsync(doc.Id, UserService.GetUser());

        await act.Should().ThrowAsync<DocumentNotInDraftStateException>()
            .WithMessage($"Document {doc.Id} is not in Draft state.");
    }
}