using FluentAssertions;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

namespace WarehouseManagementSystem.Tests.Integration;

public class DocumentIntegrationTests
{
    [Fact]
    public void Confirm_DraftDocumentWithItems_ShouldSetConfirmed()
    {
        // Arrange
        var warehouse = new Warehouse("WH1", "Main WH", "PL", "Warsaw", "Street 1");
        var zone = warehouse.AddZone("Z1", "Zone 1", TemperatureType.Ambient, true);

        var productId = Guid.NewGuid();

        var userId = Guid.NewGuid();
        var document = new Document(
            number: "DOC001",
            documentDate: DateTime.UtcNow,
            type: DocumentType.WZ,
            createdById: userId,
            sourceWarehouseId: warehouse.Id
        );

        var item = new DocumentItem(
            productId: productId,
            quantity: 20,
            sourceZoneId: zone.Id
        );

        document.AddItem(item);

        // Act
        document.Confirm(Guid.NewGuid());

        // Assert
        document.Status.Should().Be(DocumentStatus.Confirmed);
        document.ConfirmedAt.Should().NotBeNull();
        document.ConfirmedById.Should().NotBeNull();
    }

    [Fact]
    public void Confirm_ShouldThrow_WhenNoItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document(
            number: "DOC002",
            documentDate: DateTime.UtcNow,
            type: DocumentType.PZ,
            createdById: userId
        );

        // Act
        Action act = () => document.Confirm(Guid.NewGuid());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot confirm document without items.");
    }

    [Fact]
    public void AddItem_ShouldThrow_WhenDocumentNotDraft()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document(
            number: "DOC003",
            documentDate: DateTime.UtcNow,
            type: DocumentType.MM,
            createdById: userId
        );

        var item = new DocumentItem(Guid.NewGuid(), 5, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        document.AddItem(item);

        document.Confirm(Guid.NewGuid());

        // Act
        var newItem = new DocumentItem(Guid.NewGuid(), 2, null, Guid.NewGuid(), Guid.NewGuid());
        Action act = () => document.AddItem(newItem);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only draft document can be modified.");
    }

    [Fact]
    public void Cancel_DraftDocument_ShouldSetCancelled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document(
            number: "DOC004",
            documentDate: DateTime.UtcNow,
            type: DocumentType.ADJ,
            createdById: userId
        );

        // Act
        document.Cancel();

        // Assert
        document.Status.Should().Be(DocumentStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ConfirmedDocument_ShouldThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document(
            number: "DOC005",
            documentDate: DateTime.UtcNow,
            type: DocumentType.PZ,
            createdById: userId
        );
        var item = new DocumentItem(Guid.NewGuid(), 5, null, null, Guid.NewGuid());
        document.AddItem(item);
        document.Confirm(Guid.NewGuid());

        // Act
        Action act = () => document.Cancel();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Confirmed document cannot be cancelled.");
    }

    [Fact]
    public void RemoveItem_ShouldRemoveItem_WhenDraft()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document(
            number: "DOC006",
            documentDate: DateTime.UtcNow,
            type: DocumentType.MM,
            createdById: userId
        );

        var item = new DocumentItem(Guid.NewGuid(), 5, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        document.AddItem(item);

        // Act
        document.RemoveItem(item.Id);

        // Assert
        document.Items.Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_ShouldThrow_WhenNotDraft()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var document = new Document(
            number: "DOC007",
            documentDate: DateTime.UtcNow,
            type: DocumentType.MM,
            createdById: userId
        );

        var item = new DocumentItem(Guid.NewGuid(), 5, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        document.AddItem(item);
        document.Confirm(Guid.NewGuid());

        // Act
        Action act = () => document.RemoveItem(item.Id);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only draft document can be modified.");
    }

    [Theory]
    [InlineData(DocumentType.PZ)]
    [InlineData(DocumentType.WZ)]
    [InlineData(DocumentType.MM)]
    [InlineData(DocumentType.ADJ)]
    public void ValidateDocumentItem_ForEachType_ShouldThrowIfZonesMissing(DocumentType type)
    {
        // Arrange
        var productId = Guid.NewGuid();
        var item = new DocumentItem(productId, 10);

        // Act
        Action act = () => item.ValidateForDocumentType(type);

        // Assert
        if (type == DocumentType.PZ)
            act.Should().Throw<InvalidOperationException>().WithMessage("*target zone*");
        else if (type == DocumentType.WZ)
            act.Should().Throw<InvalidOperationException>().WithMessage("*source zone*");
        else if (type == DocumentType.MM)
            act.Should().Throw<InvalidOperationException>().WithMessage("*both source and target*");
        else
            act.Should().NotThrow();
    }

    [Fact]
    public void StartTransfer_ShouldSetStatusToTransfer_WhenDocumentIsConfirmed()
    {
        // Arrange
        var createdBy = Guid.NewGuid();
        var confirmedBy = Guid.NewGuid();
        var document = new Document("DOC01", DateTime.Today, DocumentType.PZ, createdBy);
        var item = new DocumentItem(Guid.NewGuid(), 5, null, null, Guid.NewGuid());
        document.AddItem(item);
        document.Confirm(confirmedBy);

        var transferUserId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        document.StartTransfer(transferUserId, now);

        // Assert
        document.Status.Should().Be(DocumentStatus.Transfer);
        document.TransferStartedById.Should().Be(transferUserId);
        document.TransferStartedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void StartTransfer_ShouldThrow_WhenDocumentIsNotConfirmed()
    {
        // Arrange
        var createdBy = Guid.NewGuid();
        var document = new Document("DOC02", DateTime.Today, DocumentType.PZ, createdBy);
        var item = new DocumentItem(Guid.NewGuid(), 5, null, null, Guid.NewGuid());
        document.AddItem(item);

        var transferUserId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        Action actDraft = () => document.StartTransfer(transferUserId, now);

        // Assert
        actDraft.Should().Throw<InvalidOperationException>()
            .WithMessage("Only confirmed document can be transferred.");

        // Confirm and cancel edge case
        document.Cancel();
        Action actCancelled = () => document.StartTransfer(transferUserId, now);

        actCancelled.Should().Throw<InvalidOperationException>()
            .WithMessage("Cancelled document cannot be transferred.");
    }
}