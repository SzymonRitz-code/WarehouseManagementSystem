using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using FluentAssertions;

namespace WarehouseManagementSystem.Tests.Domain.DocumentsDomain;

public class DocumentTests
{
    // ================= Constructor & Properties =================
    [Fact]
    public void Constructor_Should_Set_Properties_Correctly()
    {
        var number = "DOC-001";
        var documentDate = DateTime.UtcNow;
        var type = DocumentType.PZ;
        var createdById = Guid.NewGuid();
        var notes = "Some notes";

        var doc = new Document(number, documentDate, type, createdById, null, null, notes);

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

    // ================= Number & Notes Validations =================
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetNumber_Should_Throw_On_Empty(string invalidNumber)
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        Action act = () => doc.SetNumber(invalidNumber);
        act.Should().Throw<ArgumentException>().WithMessage("Document number cannot be empty.");
    }

    [Fact]
    public void SetNumber_Should_Throw_If_Too_Long()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        var longNumber = new string('A', 51);
        Action act = () => doc.SetNumber(longNumber);
        act.Should().Throw<ArgumentException>().WithMessage("Document number cannot exceed 50 characters.");
    }

    [Fact]
    public void SetNotes_Should_Throw_If_Too_Long()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        var longNotes = new string('A', 1001);
        Action act = () => doc.SetNotes(longNotes);
        act.Should().Throw<ArgumentException>().WithMessage("Notes cannot exceed 1000 characters.");
    }

    // ================= Draft Operations =================
    [Fact]
    public void ChangeDate_Should_Work_Only_In_Draft()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        var newDate = DateTime.UtcNow.AddDays(1);

        doc.ChangeDate(newDate);
        doc.DocumentDate.Should().Be(newDate);

        doc.AddItem(new DocumentItem(Guid.NewGuid(), 1));
        doc.Confirm(Guid.NewGuid());

        Action act = () => doc.ChangeDate(DateTime.UtcNow.AddDays(2));
        act.Should().Throw<InvalidOperationException>().WithMessage("Only draft document can be modified.");
    }

    [Fact]
    public void AddItem_Should_Work_Only_In_Draft()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        var item = new DocumentItem(Guid.NewGuid(), 5);

        doc.AddItem(item);
        doc.Items.Should().ContainSingle().Which.Should().Be(item);

        doc.Confirm(Guid.NewGuid());
        Action act = () => doc.AddItem(new DocumentItem(Guid.NewGuid(), 1));
        act.Should().Throw<InvalidOperationException>().WithMessage("Only draft document can be modified.");
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
        doc.Confirm(Guid.NewGuid());
        Action act = () => doc.RemoveItem(item.Id);
        act.Should().Throw<InvalidOperationException>().WithMessage("Only draft document can be modified.");
    }

    // ================= Confirm =================
    [Fact]
    public void Confirm_Should_Work_Correctly()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 5));

        var confirmedBy = Guid.NewGuid();
        doc.Confirm(confirmedBy);

        doc.Status.Should().Be(DocumentStatus.Confirmed);
        doc.ConfirmedById.Should().Be(confirmedBy);
        doc.ConfirmedAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Confirm_Should_Throw_If_No_Items()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        Action act = () => doc.Confirm(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot confirm document without items.");
    }

    // ================= Cancel =================
    [Fact]
    public void Cancel_Should_Work_From_Draft()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        doc.Cancel();
        doc.Status.Should().Be(DocumentStatus.Cancelled);

        Action act = () => doc.Cancel();
        act.Should().Throw<InvalidOperationException>().WithMessage("Document is already cancelled.");
    }

    [Fact]
    public void Cancel_Should_Throw_For_Confirmed()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 5));
        doc.Confirm(Guid.NewGuid());

        Action act = () => doc.Cancel();
        act.Should().Throw<InvalidOperationException>().WithMessage("Confirmed document cannot be cancelled.");
    }

    // ================= Transfer =================
    [Fact]
    public void Draft_Cannot_StartTransfer()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 5));

        var transferUser = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        Action act = () => doc.StartTransfer(transferUser, now);
        act.Should().Throw<InvalidOperationException>().WithMessage("Only confirmed document can be transferred.");
    }

    [Fact]
    public void Cancelled_Cannot_StartTransfer()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 5));
        doc.Cancel();

        var transferUser = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        Action act = () => doc.StartTransfer(transferUser, now);
        act.Should().Throw<InvalidOperationException>().WithMessage("Cancelled document cannot be transferred.");
    }

    [Fact]
    public void Confirmed_Can_StartTransfer()
    {
        var doc = new Document("Valid", DateTime.UtcNow, DocumentType.PZ, Guid.NewGuid());
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 5));
        doc.Confirm(Guid.NewGuid());

        var transferUser = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        doc.StartTransfer(transferUser, now);

        doc.Status.Should().Be(DocumentStatus.Transfer);
        doc.TransferStartedAt.Should().Be(now);
        doc.TransferStartedById.Should().Be(transferUser);
    }

    [Fact]
    public void CompleteTransfer_ShouldSetStatusToConfirmed_WhenDocumentIsInTransfer()
    {
        // Arrange
        var createdBy = Guid.NewGuid();
        var transferUser = Guid.NewGuid();
        var document = new Document("DOC100", DateTime.Today, DocumentType.PZ, createdBy);
        var item = new DocumentItem(Guid.NewGuid(), 5, null, null, Guid.NewGuid());
        document.AddItem(item);

        // Confirm first to allow transfer
        document.Confirm(createdBy);
        document.StartTransfer(transferUser, DateTimeOffset.UtcNow);

        var confirmedBy = Guid.NewGuid();

        // Act
        document.CompleteTransfer(confirmedBy);

        // Assert
        document.Status.Should().Be(DocumentStatus.Confirmed);
        document.ConfirmedById.Should().Be(confirmedBy);
        document.ConfirmedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CompleteTransfer_ShouldThrow_WhenDocumentIsNotInTransfer()
    {
        // Arrange
        var createdBy = Guid.NewGuid();
        var document = new Document("DOC101", DateTime.Today, DocumentType.PZ, createdBy);
        var item = new DocumentItem(Guid.NewGuid(), 5, null, null, Guid.NewGuid());
        document.AddItem(item);

        var confirmedBy = Guid.NewGuid();

        // Act & Assert
        document.Invoking(d => d.CompleteTransfer(confirmedBy))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("Only transferred document can be completed.");

        // Confirmed document (not in Transfer) also fails
        document.Confirm(createdBy);
        document.Invoking(d => d.CompleteTransfer(confirmedBy))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("Only transferred document can be completed.");
    }
}