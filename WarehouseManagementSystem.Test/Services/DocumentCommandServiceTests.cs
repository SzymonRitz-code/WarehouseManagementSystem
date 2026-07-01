using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using WarehouseManagementSystem.API.Caching;
using WarehouseManagementSystem.API.Services.AuditLogs.Command;
using WarehouseManagementSystem.API.Services.Documents.Command;
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
using WarehouseManagementSystem.API.Services.Stocks.Command;

namespace WarehouseManagementSystem.Tests.Services;

public class DocumentCommandServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IStockCommandService> _stockServiceMock = new();
    private readonly Mock<IDocumentNumberGenerator> _numberGeneratorMock = new();
    private readonly Mock<ISystemClock> _clockMock = new();
    private readonly Mock<ILogger<DocumentCommandService>> _logger = new();
    private readonly Mock<IAuditLogCommandService> _auditLogService = new();
    private readonly Mock<ICacheInvalidationService> _cacheInvalidation = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IUnitOfWorkTransaction> _transactionMock = new();

    private readonly DocumentCommandService _service;

    public DocumentCommandServiceTests()
    {
        _service = new DocumentCommandService(
            _unitOfWorkMock.Object,
            _stockServiceMock.Object,
            _numberGeneratorMock.Object,
            _clockMock.Object,
            _logger.Object,
            _auditLogService.Object,
            _cacheInvalidation.Object);
        _transactionMock
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(x => x.BeginTransactionAsync(
                It.IsAny<System.Data.IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);
        _userServiceMock.Setup(s => s.GetUser(It.IsAny<HttpContext>()))
            .Returns(new UserSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Testomir.Testowski@gmail.com", "Testomir"));
    }

    #region CreateDocumentAsync Tests

    /// <summary>
    /// Verifies that CreateDocumentAsync throws ArgumentException when document has no items.
    /// </summary>
    [Fact]
    public async Task CreateDocumentAsync_ShouldThrow_WhenNoItems()
    {
        Func<Task> act = () => _service.CreateDocumentAsync(
            DocumentType.PZ,
            _userServiceMock.Object.GetUser(default),
            Guid.NewGuid(),
            new List<DocumentItemDraft>(),
            DateTime.UtcNow);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*at least one item*");
    }

    /// <summary>
    /// Verifies that CreateDocumentAsync creates a document and saves it to the repository with correct properties.
    /// </summary>
    [Fact]
    public async Task CreateDocumentAsync_ShouldCreateDocumentAndSave()
    {
        // Arrange
        var draftItems = new List<DocumentItemDraft>
        {
            new(Guid.NewGuid(), 5, null, Guid.NewGuid(), null)
        };

        _numberGeneratorMock
            .Setup(x => x.GenerateAsync(DocumentType.PZ, It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync("DOC-001");

        // Mock repozytorium dokumentów
        var documentRepoMock = new Mock<IDocumentRepository>();
        _unitOfWorkMock.Setup(u => u.Documents).Returns(documentRepoMock.Object);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var document = await _service.CreateDocumentAsync(
            DocumentType.PZ,
            _userServiceMock.Object.GetUser(default),
            Guid.NewGuid(),
            draftItems,
            DateTime.UtcNow);

        // Assert
        documentRepoMock.Verify(r => r.Add(document), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        document.Number.Should().Be(null);
        document.Items.Should().HaveCount(1);
        document.Items.First().ProductId.Should().Be(draftItems.First().ProductId);
    }

    #endregion

    #region ConfirmDocumentAsync Tests

    /// <summary>
    /// Verifies that ConfirmDocumentAsync generates and assigns a document number and persists changes in a serializable transaction.
    /// </summary>
    [Fact]
    public async Task ConfirmDocumentAsync_ShouldGenerateNumberAndSave()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();
        var doc = new Document(
            DateTime.UtcNow,
            DocumentType.PZ,
            _userServiceMock.Object.GetUser(default),
            Guid.NewGuid(),
            null,
            null);
        doc.AddItem(new DocumentItem(productId, 5, null, zoneId, null));
        _unitOfWorkMock
            .Setup(x => x.Documents.GetDocumentWithItems(doc.Id))
            .ReturnsAsync(doc);
        _numberGeneratorMock
            .Setup(x => x.GenerateAsync(DocumentType.PZ, It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync("DOC-001");
        await _service.ConfirmDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(default));
        doc.Number.Should().Be("DOC-001");
        _unitOfWorkMock.Verify(x => x.Documents.Update(doc), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(
            x => x.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, It.IsAny<CancellationToken>()),
            Times.Once);
        _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that ConfirmDocumentAsync throws CannotConfirmEmptyDocumentException when attempting to confirm a document without items.
    /// </summary>
    [Fact]
    public async Task ConfirmDocumentAsync_ShouldThrowException_WhenDocumentIsEmpty()
    {
        var doc = new Document(
            DateTime.UtcNow,
            DocumentType.PZ,
            _userServiceMock.Object.GetUser(default),
            Guid.NewGuid(),
            null,
            null);
        _unitOfWorkMock
            .Setup(x => x.Documents.GetDocumentWithItems(doc.Id))
            .ReturnsAsync(doc);
        _numberGeneratorMock
            .Setup(x => x.GenerateAsync(DocumentType.PZ, It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync("DOC-001");
        Func<Task> act = () => _service.ConfirmDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(default));
        await act.Should()
            .ThrowAsync<CannotConfirmEmptyDocumentException>()
            .WithMessage($"Document {doc.Id} cannot be confirmed without items.");
    }

    /// <summary>
    /// Verifies that ConfirmDocumentAsync calls IncreaseStockAsync for PZ (intake) documents with correct parameters.
    /// </summary>
    [Fact]
    public async Task ConfirmDocumentAsync_ShouldIncreaseStock_ForPZ()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();

        var doc = new Document(
            DateTime.UtcNow,
            DocumentType.PZ,
            _userServiceMock.Object.GetUser(default),
            warehouseId,
            null,
            null);

        doc.AddItem(new DocumentItem(productId, 5, null, zoneId, null));

        _unitOfWorkMock
            .Setup(x => x.Documents.GetDocumentWithItems(doc.Id))
            .ReturnsAsync(doc);
        _numberGeneratorMock
            .Setup(x => x.GenerateAsync(DocumentType.PZ, It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync("DOC-001");

        await _service.ConfirmDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(default));

        _stockServiceMock.Verify(x => x.IncreaseStockAsync(
            productId,
            warehouseId,
            zoneId,
            5,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(x => x.Documents.Update(doc), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that ConfirmDocumentAsync calls DecreaseStockAsync for WZ (withdrawal) documents with correct parameters.
    /// </summary>
    [Fact]
    public async Task ConfirmDocumentAsync_ShouldDecreaseStock_ForWZ()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();

        var doc = new Document(
            DateTime.UtcNow,
            DocumentType.WZ,
            _userServiceMock.Object.GetUser(default),
            warehouseId,
            null,
            null);

        doc.AddItem(new DocumentItem(productId, 5, null, zoneId, null));

        _unitOfWorkMock.Setup(x => x.Documents.GetDocumentWithItems(doc.Id)).ReturnsAsync(doc);
        _numberGeneratorMock
            .Setup(x => x.GenerateAsync(DocumentType.WZ, It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync("DOC-001");

        await _service.ConfirmDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(default));

        _stockServiceMock.Verify(x => x.DecreaseStockAsync(
            productId,
            warehouseId,
            zoneId,
            5,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that ConfirmDocumentAsync calls MoveStockAsync for MM (transfer) documents with correct parameters.
    /// </summary>
    [Fact]
    public async Task ConfirmDocumentAsync_ShouldMoveStock_ForMM()
    {
        var productId = Guid.NewGuid();
        var sourceWarehouse = Guid.NewGuid();
        var targetWarehouse = Guid.NewGuid();
        var sourceZone = Guid.NewGuid();
        var targetZone = Guid.NewGuid();

        var doc = new Document(
            DateTime.UtcNow,
            DocumentType.MM,
            _userServiceMock.Object.GetUser(default),
            sourceWarehouse,
            targetWarehouse,
            null);

        doc.AddItem(new DocumentItem(productId, 5, null, sourceZone, targetZone));

        _unitOfWorkMock
            .Setup(x => x.Documents.GetDocumentWithItems(doc.Id))
            .ReturnsAsync(doc);
        _numberGeneratorMock
            .Setup(x => x.GenerateAsync(DocumentType.MM, It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync("DOC-001");

        await _service.ConfirmDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(default));

        _stockServiceMock.Verify(x => x.MoveStockAsync(
            productId,
            sourceWarehouse,
            sourceZone,
            targetWarehouse,
            targetZone,
            5,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that ConfirmDocumentAsync throws DocumentNotFoundException when document does not exist.
    /// </summary>
    [Fact]
    public async Task ConfirmDocumentAsync_ShouldThrow_WhenDocumentNotFound()
    {
        _unitOfWorkMock
            .Setup(x => x.Documents.GetDocumentWithItems(It.IsAny<Guid>()))
            .ReturnsAsync((Document?)null);

        Func<Task> act = () => _service.ConfirmDocumentAsync(Guid.NewGuid(), _userServiceMock.Object.GetUser(default));

        await act.Should()
            .ThrowAsync<DocumentNotFoundException>()
            .WithMessage("*was not found*");
    }

    #endregion

    #region CancelDocumentAsync Tests

    /// <summary>
    /// Verifies that CancelDocumentAsync releases stock reservations for WZ (withdrawal) documents.
    /// </summary>
    [Fact]
    public async Task CancelDocumentAsync_ShouldReleaseReservations_ForWZ()
    {
        var doc = new Document(
            DateTime.UtcNow,
            DocumentType.WZ,
            _userServiceMock.Object.GetUser(default),
            Guid.NewGuid(),
            null,
            null);

        var reservation = new StockReservation(
            Guid.NewGuid(),
            5,
            "TEST",
            _userServiceMock.Object.GetUser(default));

        _unitOfWorkMock
            .Setup(x => x.Documents.FindAsync(doc.Id))
            .ReturnsAsync(doc);

        _unitOfWorkMock
            .Setup(x => x.Stocks.GetActiveReservationsByDocumentIdAsync(doc.Id))
            .ReturnsAsync(new List<StockReservation> { reservation });

        await _service.CancelDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(default));

        _stockServiceMock.Verify(x =>
            x.ReleaseReservationAsync(reservation.StockId, reservation.Id, It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(x => x.Documents.Update(doc), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that CancelDocumentAsync successfully cancels a document when no reservations exist and commits the transaction.
    /// </summary>
    [Fact]
    public async Task CancelDocumentAsync_ShouldCancelDocument_WhenNoReservations()
    {
        var doc = new Document(
            DateTime.UtcNow,
            DocumentType.WZ,
            _userServiceMock.Object.GetUser(default),
            Guid.NewGuid(),
            null,
            null);

        _unitOfWorkMock
            .Setup(x => x.Documents.FindAsync(doc.Id))
            .ReturnsAsync(doc);

        _unitOfWorkMock
            .Setup(x => x.Stocks.GetActiveReservationsByDocumentIdAsync(doc.Id))
            .ReturnsAsync(new List<StockReservation>());

        await _service.CancelDocumentAsync(doc.Id, _userServiceMock.Object.GetUser(default));

        _stockServiceMock.Verify(
            x => x.ReleaseReservationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(x => x.Documents.Update(doc), Times.Once);
        _unitOfWorkMock.Verify(
            x => x.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, It.IsAny<CancellationToken>()),
            Times.Once);
        _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
