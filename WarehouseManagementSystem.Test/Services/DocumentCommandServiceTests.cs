using FluentAssertions;
using Moq;
using WarehouseManagementSystem.API.Services.Documents;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;
using WarehouseManagementSystem.Domain.Services;
using WarehouseManagementSystem.Domain.ValueObjects;
using WarehouseManagementSystem.Infrastructure.Services;

namespace WarehouseManagementSystem.Tests.Services;

public class DocumentCommandServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IStockService> _stockServiceMock = new();
    private readonly Mock<IDocumentNumberGenerator> _numberGeneratorMock = new();
    private readonly Mock<ISystemClock> _clockMock = new();

    private readonly DocumentCommandService _service;

    public DocumentCommandServiceTests()
    {
        _service = new DocumentCommandService(
            _unitOfWorkMock.Object,
            _stockServiceMock.Object,
            _numberGeneratorMock.Object,
            _clockMock.Object);
    }

    [Fact]
    public async Task CreateDocumentAsync_ShouldThrow_WhenNoItems()
    {
        Func<Task> act = () => _service.CreateDocumentAsync(
            DocumentType.PZ,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new List<DocumentItemDraft>(),
            DateTime.UtcNow);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*at least one item*");
    }

    [Fact]
    public async Task CreateDocumentAsync_ShouldCreateDocumentAndSave()
    {
        var draftItems = new List<DocumentItemDraft>
        {
            new(Guid.NewGuid(), 5, null, Guid.NewGuid(), null)
        };

        _numberGeneratorMock
            .Setup(x => x.GenerateAsync(
                DocumentType.PZ,
                It.IsAny<Guid>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync("DOC-001");

        var document = await _service.CreateDocumentAsync(
            DocumentType.PZ,
            Guid.NewGuid(),
            Guid.NewGuid(),
            draftItems,
            DateTime.UtcNow);

        _unitOfWorkMock.Verify(x => x.Documents.Add(document), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        document.Number.Should().Be("DOC-001");
        document.Items.Should().HaveCount(1);
        document.Items.First().ProductId.Should().Be(draftItems.First().ProductId);
    }

    [Fact]
    public async Task StartTransferAsync_ShouldStartTransferAndSave()
    {
        var doc = new Document(
            "DOC-001",
            DateTime.UtcNow,
            DocumentType.PZ,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null);

        var now = DateTimeOffset.UtcNow;

        _unitOfWorkMock
            .Setup(x => x.Documents.FindAsync(doc.Id))
            .ReturnsAsync(doc);

        _clockMock
            .Setup(x => x.UtcNow)
            .Returns(now);

        await _service.StartTransferAsync(doc.Id, Guid.NewGuid());

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmDocumentAsync_ShouldIncreaseStock_ForPZ()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();

        var doc = new Document(
            "DOC-001",
            DateTime.UtcNow,
            DocumentType.PZ,
            Guid.NewGuid(),
            warehouseId,
            null,
            null);

        doc.AddItem(new DocumentItem(productId, 5, null, zoneId, null));

        _unitOfWorkMock
            .Setup(x => x.Documents.FindAsync(doc.Id))
            .ReturnsAsync(doc);

        await _service.ConfirmDocumentAsync(doc.Id, Guid.NewGuid());

        _stockServiceMock.Verify(x => x.IncreaseStockAsync(
            productId,
            warehouseId,
            zoneId,
            5,
            null), Times.Once);

        _unitOfWorkMock.Verify(x => x.Documents.Update(doc), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmDocumentAsync_ShouldDecreaseStock_ForWZ()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();

        var doc = new Document(
            "DOC-001",
            DateTime.UtcNow,
            DocumentType.WZ,
            Guid.NewGuid(),
            warehouseId,
            null,
            null);

        doc.AddItem(new DocumentItem(productId, 5, null, zoneId, null));

        _unitOfWorkMock
            .Setup(x => x.Documents.FindAsync(doc.Id))
            .ReturnsAsync(doc);

        await _service.ConfirmDocumentAsync(doc.Id, Guid.NewGuid());

        _stockServiceMock.Verify(x => x.DecreaseStockAsync(
            productId,
            warehouseId,
            zoneId,
            5,
            null), Times.Once);
    }

    [Fact]
    public async Task ConfirmDocumentAsync_ShouldMoveStock_ForMM()
    {
        var productId = Guid.NewGuid();
        var sourceWarehouse = Guid.NewGuid();
        var targetWarehouse = Guid.NewGuid();
        var sourceZone = Guid.NewGuid();
        var targetZone = Guid.NewGuid();

        var doc = new Document(
            "DOC-001",
            DateTime.UtcNow,
            DocumentType.MM,
            Guid.NewGuid(),
            sourceWarehouse,
            targetWarehouse,
            null);

        doc.AddItem(new DocumentItem(productId, 5, null, sourceZone, targetZone));

        _unitOfWorkMock
            .Setup(x => x.Documents.FindAsync(doc.Id))
            .ReturnsAsync(doc);

        await _service.ConfirmDocumentAsync(doc.Id, Guid.NewGuid());

        _stockServiceMock.Verify(x => x.MoveStockAsync(
            productId,
            sourceWarehouse,
            sourceZone,
            targetWarehouse,
            targetZone,
            5,
            null), Times.Once);
    }

    [Fact]
    public async Task ConfirmDocumentAsync_ShouldThrow_WhenDocumentNotFound()
    {
        _unitOfWorkMock
            .Setup(x => x.Documents.FindAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Document?)null);

        Func<Task> act = () => _service.ConfirmDocumentAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Document not found*");
    }

    [Fact]
    public async Task CancelDocumentAsync_ShouldReleaseReservations_ForWZ()
    {
        var doc = new Document(
            "DOC-001",
            DateTime.UtcNow,
            DocumentType.WZ,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null);

        var reservation = new StockReservation(
            Guid.NewGuid(),
            5,
            "TEST",
            Guid.NewGuid());

        _unitOfWorkMock
            .Setup(x => x.Documents.FindAsync(doc.Id))
            .ReturnsAsync(doc);

        _unitOfWorkMock
            .Setup(x => x.Stocks.GetActiveReservationsByDocumentIdAsync(doc.Id))
            .ReturnsAsync(new List<StockReservation> { reservation });

        await _service.CancelDocumentAsync(doc.Id);

        _stockServiceMock.Verify(x =>
            x.ReleaseReservationAsync(reservation.StockId, reservation.Id),
            Times.Once);

        _unitOfWorkMock.Verify(x => x.Documents.Update(doc), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelDocumentAsync_ShouldCancelDocument_WhenNoReservations()
    {
        var doc = new Document(
            "DOC-001",
            DateTime.UtcNow,
            DocumentType.WZ,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null);

        _unitOfWorkMock
            .Setup(x => x.Documents.FindAsync(doc.Id))
            .ReturnsAsync(doc);

        _unitOfWorkMock
            .Setup(x => x.Stocks.GetActiveReservationsByDocumentIdAsync(doc.Id))
            .ReturnsAsync(new List<StockReservation>());

        await _service.CancelDocumentAsync(doc.Id);

        _stockServiceMock.Verify(
            x => x.ReleaseReservationAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);

        _unitOfWorkMock.Verify(x => x.Documents.Update(doc), Times.Once);
    }
}