using FluentAssertions;
using Microsoft.AspNetCore.Http;
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

// TODO refactor testów pod nowy przepływ Dokumentów - Dokumenty wykonują transfer w Confirm. StartTransferAsync jest wywoływane w Confirm.


/// <summary>
/// Tests for DocumentIntegrationTests — application layer orchestration.
/// Domain invariant tests (pure) live in DocumentDomainTests.
/// </summary>
public class DocumentIntegrationTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IStockService> _stockServiceMock = new();
    private readonly Mock<ISystemClock> _clockMock = new();
    private readonly Mock<IDocumentRepository> _documentRepoMock = new();
    private readonly Mock<ILogger<DocumentCommandService>> _logger = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IDocumentNumberGenerator> _numberGeneratorMock = new();

    private readonly DocumentCommandService _service;

    public DocumentIntegrationTests()
    {
        _service = new DocumentCommandService(
            _unitOfWorkMock.Object,
            _stockServiceMock.Object,
            _numberGeneratorMock.Object,
            _clockMock.Object,
            _logger.Object,
            _auditLogService.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.Documents).Returns(_documentRepoMock.Object);
        _userServiceMock.Setup(s => s.GetUser(It.IsAny<HttpContext>()))
            .Returns(new UserSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Testomir.Testowski@gmail.com", "Testomir"));
    }

    // =========================================================
    // Helpers / Builders
    // =========================================================

    private DocumentItemDraft AnyItemDraft(
        Guid? productId = null,
        Guid? sourceZone = null,
        int quantity = 5)
        => new(productId ?? Guid.NewGuid(), quantity, null, sourceZone ?? Guid.NewGuid(), null);

    /// <summary>
    /// Builds a Draft document with one item — ready for service-level tests.
    /// Does NOT go through the service so Number is guaranteed null.
    /// </summary>
    private Document DraftDocumentWithItem(
        DocumentType type = DocumentType.PZ,
        Guid? warehouseId = null,
        Guid? targetWareId = null)
    {
        var doc = new Document(
            documentDate: DateTime.UtcNow,
            type: type,
            createdByUser: _userServiceMock.Object.GetUser(default),
            sourceWarehouseId: warehouseId ?? Guid.NewGuid(),
            targetWarehouseId: targetWareId,
            notes: null);

        var zoneId = Guid.NewGuid();
        var targetZone = targetWareId.HasValue ? Guid.NewGuid() : (Guid?)null;
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 5, null, zoneId, targetZone));

        return doc;
    }

    private void SetupNumberGenerator(string number = "PZ/2024/001")
        => _numberGeneratorMock
            .Setup(x => x.GenerateAsync(
                It.IsAny<DocumentType>(),
                It.IsAny<Guid?>(),
                It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(number);

    private void SetupDocumentGetDocumentWithItems(Document doc)
        => _unitOfWorkMock
            .Setup(x => x.Documents.GetDocumentWithItems(doc.Id))
            .ReturnsAsync(doc);
    private void SetupDocumentFind(Document doc)
    => _unitOfWorkMock
        .Setup(x => x.Documents.FindAsync(doc.Id))
        .ReturnsAsync(doc);

    // =========================================================
    // CreateDocumentAsync
    // =========================================================

    [Fact]
    public async Task CreateDocument_Throws_WhenItemsEmpty()
    {
        Func<Task> act = () => _service.CreateDocumentAsync(
            DocumentType.PZ, _userServiceMock.Object.GetUser(default), Guid.NewGuid(),
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
        var doc = await _service.CreateDocumentAsync(
            DocumentType.PZ, _userServiceMock.Object.GetUser(default), Guid.NewGuid(), items, DateTime.UtcNow);

        // Assert — invariant: Number MUST be null after Create
        doc.Status.Should().Be(DocumentStatus.Draft);
        doc.Number.Should().BeNull("Number is assigned only during Confirm, never during Create");
        doc.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateDocument_PersistsDocument_ExactlyOnce()
    {
        var items = new[] { AnyItemDraft() };

        var doc = await _service.CreateDocumentAsync(
            DocumentType.PZ, _userServiceMock.Object.GetUser(default), Guid.NewGuid(), items, DateTime.UtcNow);

        _documentRepoMock.Verify(r => r.Add(It.Is<Document>(d => d.Id == doc.Id)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateDocument_StoresCreatorSnapshot_Correctly()
    {
        // Arrange
        var creator = _userServiceMock.Object.GetUser(default);
        var items = new[] { AnyItemDraft() };
        var documentRepoMock = new Mock<IDocumentRepository>();
        _unitOfWorkMock.Setup(u => u.Documents).Returns(documentRepoMock.Object);
        // Act
        var doc = await _service.CreateDocumentAsync(
            DocumentType.PZ, creator, Guid.NewGuid(), items, DateTime.UtcNow);

        // Assert — snapshot, not FK
        doc.CreatedByUser.Should().NotBeNull();
        doc.CreatedByUser.Name.Should().Be("Testomir");
        doc.CreatedByUser.Email.Should().Be("Testomir.Testowski@gmail.com");
    }

    // =========================================================
    // ConfirmDocumentAsync — Number lifecycle
    // =========================================================

    [Fact]
    public async Task ConfirmDocumentAsync_ShouldAssignNumber_FromGenerator()
    {
        // Arrange
        SetupNumberGenerator("PZ/2024/001");
        var doc = DraftDocumentWithItem(DocumentType.PZ);
        SetupDocumentGetDocumentWithItems(doc);

        // Act
        await _service.ConfirmDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(default));

        // Assert — core invariant
        doc.Number.Should().Be("PZ/2024/001");
    }

    [Fact]
    public async Task ConfirmDocument_SetsStatusToConfirmed()
    {
        SetupNumberGenerator();
        var doc = DraftDocumentWithItem();
        SetupDocumentGetDocumentWithItems(doc);

        await _service.ConfirmDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(default));

        doc.Status.Should().Be(DocumentStatus.Confirmed);
    }

    [Fact]
    public async Task ConfirmDocument_StoresConfirmedBySnapshot()
    {
        SetupNumberGenerator();
        var confirmedBy = _userServiceMock.Object.GetUser(default);
        var doc = DraftDocumentWithItem();
        SetupDocumentGetDocumentWithItems(doc);

        await _service.ConfirmDocumentAsync(doc.Id, confirmedBy);

        doc.ConfirmedByUser.Should().NotBeNull();
        doc.ConfirmedByUser.Name.Should().Be("Testomir");
        doc.ConfirmedByUser.Email.Should().Be("Testomir.Testowski@gmail.com");
    }

    [Fact]
    public async Task ConfirmDocument_Throws_WhenDocumentNotFound()
    {
        _unitOfWorkMock.Setup(u => u.Documents.GetDocumentWithItems(It.IsAny<Guid>())).ReturnsAsync((Document?)null);

        Func<Task> act = () => _service.ConfirmDocumentAsync(Guid.NewGuid(), _userServiceMock.Object.GetUser(default));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Document not found*");
    }

    [Fact]
    public async Task ConfirmDocument_Throws_WhenAlreadyConfirmed()
    {
        // Arrange — simulate already-confirmed document (bypassing service)
        SetupNumberGenerator("PZ/2024/001");
        var doc = DraftDocumentWithItem();
        SetupDocumentGetDocumentWithItems(doc);
        await _service.ConfirmDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(default)); // first confirm

        // second confirm attempt on same doc
        Func<Task> act = () => _service.ConfirmDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(default));

        await act.Should().ThrowAsync<DocumentNotInDraftStateException>()
            .WithMessage($"Document {doc.Id} is not in Draft state.");
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
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, _userServiceMock.Object.GetUser(default), warehouseId, null, null);
        doc.AddItem(new DocumentItem(productId, 5, null, zoneId, null));
        SetupDocumentGetDocumentWithItems(doc);

        await _service.ConfirmDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(default));

        _stockServiceMock.Verify(s => s.IncreaseStockAsync(productId, warehouseId, zoneId, 5, null), Times.Once);
    }

    [Fact]
    public async Task ConfirmDocument_DecreasesStock_ForWZ()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();

        SetupNumberGenerator();
        var doc = new Document(DateTime.UtcNow, DocumentType.WZ, _userServiceMock.Object.GetUser(default), warehouseId, null, null);
        doc.AddItem(new DocumentItem(productId, 10, null, zoneId, null));
        SetupDocumentGetDocumentWithItems(doc);

        await _service.ConfirmDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(default));

        _stockServiceMock.Verify(s => s.DecreaseStockAsync(productId, warehouseId, zoneId, 10, null), Times.Once);
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
        var doc = new Document(DateTime.UtcNow, DocumentType.MM, _userServiceMock.Object.GetUser(default), sourceWare, targetWare, null);
        doc.AddItem(new DocumentItem(productId, 3, null, sourceZone, targetZone));
        SetupDocumentGetDocumentWithItems(doc);

        await _service.ConfirmDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(default));

        _stockServiceMock.Verify(s => s.MoveStockAsync(
            productId, sourceWare, sourceZone, targetWare, targetZone, 3, null), Times.Once);
    }

    [Fact]
    public async Task ConfirmDocument_Throws_ForMM_WhenTargetWarehouseMissing()
    {
        SetupNumberGenerator();
        // MM without targetWarehouseId
        var doc = new Document(DateTime.UtcNow, DocumentType.MM, _userServiceMock.Object.GetUser(default), Guid.NewGuid(), null, null);
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 1, null, Guid.NewGuid(), Guid.NewGuid()));
        SetupDocumentGetDocumentWithItems(doc);

        Func<Task> act = () => _service.ConfirmDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(default));

        await act.Should().ThrowAsync<MissingTargetWarehouseForMmDocumentException>()
            .WithMessage($"Document {doc.Id} requires a target warehouse for MM confirmation.");
    }

    [Fact]
    public async Task ConfirmDocument_CallsNumberGenerator_WithCorrectParameters()
    {
        var warehouseId = Guid.NewGuid();
        var documentDate = new DateTime(2024, 6, 15);

        SetupNumberGenerator("PZ/2024/001");
        var doc = new Document(documentDate, DocumentType.PZ, _userServiceMock.Object.GetUser(default), warehouseId, null, null);
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 1, null, Guid.NewGuid(), null));
        SetupDocumentGetDocumentWithItems(doc);

        await _service.ConfirmDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(default));

        _numberGeneratorMock.Verify(g => g.GenerateAsync(
            DocumentType.PZ,
            warehouseId,
            documentDate), Times.Once);
    }

    // =========================================================
    // StartTransferAsync
    // =========================================================

    //[Fact]
    //public async Task StartTransfer_Throws_WhenDocumentInDraft()
    //{
    //    var doc = DraftDocumentWithItem();
    //    SetupDocumentGetDocumentWithItems(doc);

    //    Func<Task> act = () => _service.StartTransferAsync(doc.Id, Guid.NewGuid());

    //    await act.Should().ThrowAsync<DocumentNotInDraftStateException>()
    //        .WithMessage($"Document {doc.Id} is not in Draft state.");
    //}

    //[Fact]
    //public async Task StartTransfer_SetsStatusToTransfer_WhenConfirmed()
    //{
    //    var doc = DraftDocumentWithItem();
    //    doc.SetNumber("PZ/2024/001"); // simulate prior confirm (number must be set)
    //    doc.Confirm(_userServiceMock.Object.GetUser(default));
    //    SetupDocumentGetDocumentWithItems(doc);

    //    var now = DateTimeOffset.UtcNow;
    //    _clockMock.Setup(c => c.UtcNow).Returns(now);

    //    await _service.StartTransferAsync(doc.Id, Guid.NewGuid());

    //    doc.Status.Should().Be(DocumentStatus.Transfer);
    //    doc.TransferStartedAt.Should().Be(now);
    //}

    //[Fact]
    //public async Task StartTransfer_Throws_WhenDocumentCancelled()
    //{
    //    var doc = DraftDocumentWithItem();
    //    doc.Cancel(_userServiceMock.Object.GetUser(default));
    //    SetupDocumentGetDocumentWithItems(doc);

    //    Func<Task> act = () => _service.StartTransferAsync(doc.Id, Guid.NewGuid());

    //    await act.Should().ThrowAsync<DocumentNotInDraftStateException>()
    //        .WithMessage($"Document {doc.Id} is not in Draft state.");
    //}

    //[Fact]
    //public async Task StartTransfer_SavesChanges_ExactlyOnce()
    //{
    //    var doc = DraftDocumentWithItem();
    //    doc.SetNumber("PZ/2024/001");
    //    doc.Confirm(_userServiceMock.Object.GetUser(default));
    //    SetupDocumentGetDocumentWithItems(doc);
    //    _clockMock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

    //    await _service.StartTransferAsync(doc.Id, Guid.NewGuid());

    //    _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    //}

    // =========================================================
    // CancelDocumentAsync
    // =========================================================

    [Fact]
    public async Task CancelDocument_SetsStatusToCancelled()
    {
        var doc = DraftDocumentWithItem(DocumentType.WZ);
        SetupDocumentFind(doc);
        _unitOfWorkMock.Setup(u => u.Stocks.GetActiveReservationsByDocumentIdAsync(doc.Id))
            .ReturnsAsync(new List<StockReservation>());

        await _service.CancelDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));

        doc.Status.Should().Be(DocumentStatus.Cancelled);
    }

    [Fact]
    public async Task CancelDocument_ReleasesReservations_ForWZ()
    {
        var doc = DraftDocumentWithItem(DocumentType.WZ);
        SetupDocumentFind(doc);

        var reservation = new StockReservation(Guid.NewGuid(), 5, "TEST", _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));
        _unitOfWorkMock.Setup(u => u.Stocks.GetActiveReservationsByDocumentIdAsync(doc.Id))
            .ReturnsAsync(new List<StockReservation> { reservation });



        await _service.CancelDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));

        _stockServiceMock.Verify(s => s.ReleaseReservationAsync(reservation.StockId, reservation.Id), Times.Once);
        doc.Status.Should().Be(DocumentStatus.Cancelled);
    }

    [Fact]
    public async Task CancelDocument_DoesNotReleaseReservations_WhenNoneExist()
    {
        var doc = DraftDocumentWithItem(DocumentType.WZ);
        SetupDocumentFind(doc);
        _unitOfWorkMock.Setup(u => u.Stocks.GetActiveReservationsByDocumentIdAsync(doc.Id))
            .ReturnsAsync(new List<StockReservation>());

        await _service.CancelDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));

        _stockServiceMock.Verify(s =>
            s.ReleaseReservationAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CancelDocument_Throws_WhenAlreadyCancelled()
    {
        var doc = DraftDocumentWithItem(DocumentType.WZ);
        doc.Cancel(_userServiceMock.Object.GetUser(default)); // first cancel
        SetupDocumentFind(doc);
        _unitOfWorkMock.Setup(u => u.Stocks.GetActiveReservationsByDocumentIdAsync(doc.Id))
            .ReturnsAsync(new List<StockReservation>());

        Func<Task> act = () => _service.CancelDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(It.IsAny<HttpContext>()));

        await act.Should().ThrowAsync<DocumentAlreadyCancelledException>()
            .WithMessage($"Document {doc.Id} is already cancelled.");
    }

    [Fact]
    public async Task CancelDocument_Throws_WhenConfirmed()
    {
        var doc = DraftDocumentWithItem();
        doc.SetNumber("PZ/2024/001");
        doc.Confirm(_userServiceMock.Object.GetUser(default));
        SetupDocumentFind(doc);
        _unitOfWorkMock.Setup(u => u.Stocks.GetActiveReservationsByDocumentIdAsync(doc.Id))
            .ReturnsAsync(new List<StockReservation>());

        Func<Task> act = () => _service.CancelDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(default));

        await act.Should().ThrowAsync<DocumentNotInDraftStateException>()
            .WithMessage($"Document {doc.Id} is not in Draft state.");
    }
}
