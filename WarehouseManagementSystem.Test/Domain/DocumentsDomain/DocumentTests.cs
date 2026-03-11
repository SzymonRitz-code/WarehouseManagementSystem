using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using FluentAssertions;

namespace WarehouseManagementSystem.Tests.Domain.DocumentsDomain;

public class DocumentTests
{
    [Fact]
    public void Constructor_Should_Set_Properties_Correctly()
    {
        // Arrange
        var number = "DOC-001";
        var documentDate = DateTime.UtcNow;
        var type = DocumentType.PZ;
        var createdById = Guid.NewGuid();
        var notes = "Some notes";

        // Act
        var doc = new Document(number, documentDate, type, createdById, null, null, notes);

        // Assert
        doc.Id.Should().NotBe(Guid.Empty);
        doc.Number.Should().Be(number);
        doc.DocumentDate.Should().Be(documentDate);
        doc.Type.Should().Be(type);
        doc.Status.Should().Be(DocumentStatus.Draft);
        doc.CreatedById.Should().Be(createdById);
        doc.Notes.Should().Be(notes);
        doc.CreatedAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
        doc.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetNumber_Should_Throw_On_Empty(string invalidNumber)
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());

        Action act = () => doc.SetNumber(invalidNumber);

        act.Should().Throw<ArgumentException>()
           .WithMessage("Document number cannot be empty.");
    }

    [Fact]
    public void SetNumber_Should_Throw_If_Too_Long()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        var longNumber = new string('A', 51);

        Action act = () => doc.SetNumber(longNumber);

        act.Should().Throw<ArgumentException>()
           .WithMessage("Document number cannot exceed 50 characters.");
    }

    [Fact]
    public void SetNotes_Should_Throw_If_Too_Long()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        var longNotes = new string('A', 1001);

        Action act = () => doc.SetNotes(longNotes);

        act.Should().Throw<ArgumentException>()
           .WithMessage("Notes cannot exceed 1000 characters.");
    }

    [Fact]
    public void ChangeDate_Should_Work_Only_In_Draft()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        var newDate = DateTime.UtcNow.AddDays(1);

        doc.ChangeDate(newDate);

        doc.DocumentDate.Should().Be(newDate);

        var confirmedBy = Guid.NewGuid();
        doc.Confirm(confirmedBy);

        Action act = () => doc.ChangeDate(DateTime.UtcNow.AddDays(2));
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Only draft document can be modified.");
    }

    [Fact]
    public void AddItem_Should_Add_Item_Only_In_Draft()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        var item = new DocumentItem(Guid.NewGuid(), 5);

        doc.AddItem(item);
        doc.Items.Should().ContainSingle().Which.Should().Be(item);

        var confirmedBy = Guid.NewGuid();
        doc.Confirm(confirmedBy);

        Action act = () => doc.AddItem(new DocumentItem(Guid.NewGuid(), 1));
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Only draft document can be modified.");
    }

    [Fact]
    public void RemoveItem_Should_Work_Only_In_Draft()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        var item = new DocumentItem(Guid.NewGuid(), 5);
        doc.AddItem(item);

        doc.RemoveItem(item.Id);
        doc.Items.Should().BeEmpty();

        doc.AddItem(item);
        var confirmedBy = Guid.NewGuid();
        doc.Confirm(confirmedBy);

        Action act = () => doc.RemoveItem(item.Id);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Only draft document can be modified.");
    }

    [Fact]
    public void StartTransfer_Should_Work_Only_For_Confirmed()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        var item = new DocumentItem(Guid.NewGuid(), 5);
        doc.AddItem(item);
        var confirmedBy = Guid.NewGuid();
        doc.Confirm(confirmedBy);

        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();

        doc.StartTransfer(userId, now);

        doc.Status.Should().Be(DocumentStatus.Transfer);
        doc.TransferStartedAt.Should().Be(now);
        doc.TransferStartedById.Should().Be(userId);

        var invalidDoc = new Document("D2", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        Action act = () => invalidDoc.StartTransfer(Guid.NewGuid(), now);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Only confirmed document can be transferred.");
    }

    [Fact]
    public void Confirm_Should_Work_Correctly()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        var item = new DocumentItem(Guid.NewGuid(), 5);
        doc.AddItem(item);

        var confirmedById = Guid.NewGuid();
        doc.Confirm(confirmedById);

        doc.Status.Should().Be(DocumentStatus.Confirmed);
        doc.ConfirmedById.Should().Be(confirmedById);
        doc.ConfirmedAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Confirm_Should_Throw_If_No_Items()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());

        Action act = () => doc.Confirm(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Cannot confirm document without items.");
    }

    [Fact]
    public void Cancel_Should_Work_Correctly()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());

        doc.Cancel();
        doc.Status.Should().Be(DocumentStatus.Cancelled);

        Action act = () => doc.Cancel();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Document is already cancelled.");

        var confirmedBy = Guid.NewGuid();
        doc.Confirm(confirmedBy);

        Action act2 = () => doc.Cancel();
        act2.Should().Throw<InvalidOperationException>()
            .WithMessage("Confirmed document cannot be cancelled.");
    }
}