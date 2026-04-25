using FluentAssertions;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Tests.Domain.DocumentsDomain;

public class DocumentTests
{
    private static UserSnapshot AnyUser()
    => new(Guid.NewGuid(), "jan@wms.pl", "Jan Kowalski");

    private static Document DraftWithItem()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, AnyUser(), Guid.NewGuid());
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 1, null, Guid.NewGuid(), null));
        return doc;
    }
    // ================= Constructor & Properties =================
    [Fact]
    public void Constructor_Should_Set_Properties_Correctly()
    {
        var documentDate = DateTime.UtcNow;
        var type = DocumentType.PZ;
        var createdBy = UserService.GetUser();
        var notes = "Some notes";

        var doc = new Document(documentDate, type, createdBy, null, null, notes);

        doc.Id.Should().NotBe(Guid.Empty);
        doc.DocumentDate.Should().Be(documentDate);
        doc.Type.Should().Be(type);
        doc.Status.Should().Be(DocumentStatus.Draft);
        doc.CreatedByUser.Should().Be(createdBy);
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
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, UserService.GetUser());
        Action act = () => doc.SetNumber(invalidNumber);
        act.Should().Throw<ArgumentException>().WithMessage("Document number cannot be empty.");
    }

    [Fact]
    public void SetNumber_Should_Throw_If_Too_Long()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, UserService.GetUser());
        var longNumber = new string('A', 51);
        Action act = () => doc.SetNumber(longNumber);
        act.Should().Throw<ArgumentException>().WithMessage("Document number cannot exceed 50 characters.");
    }

    [Fact]
    public void SetNotes_Should_Throw_If_Too_Long()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, UserService.GetUser());
        var longNotes = new string('A', 1001);
        Action act = () => doc.SetNotes(longNotes);
        act.Should().Throw<ArgumentException>().WithMessage("Notes cannot exceed 1000 characters.");
    }

    // ================= Draft Operations =================
    [Fact]
    public void ChangeDate_Should_Work_Only_In_Draft()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, UserService.GetUser());
        var newDate = DateTime.UtcNow.AddDays(1);

        doc.ChangeDate(newDate);
        doc.DocumentDate.Should().Be(newDate);

        doc.AddItem(new DocumentItem(Guid.NewGuid(), 1));
        doc.Confirm(UserService.GetUser());

        Action act = () => doc.ChangeDate(DateTime.UtcNow.AddDays(2));
        act.Should().Throw<InvalidOperationException>().WithMessage("Only draft document can be modified.");
    }

    [Fact]
    public void AddItem_Should_Work_Only_In_Draft()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, UserService.GetUser());
        var item = new DocumentItem(Guid.NewGuid(), 5);

        doc.AddItem(item);
        doc.Items.Should().ContainSingle().Which.Should().Be(item);

        doc.Confirm(UserService.GetUser());
        Action act = () => doc.AddItem(new DocumentItem(Guid.NewGuid(), 1));
        act.Should().Throw<InvalidOperationException>().WithMessage("Only draft document can be modified.");
    }

    [Fact]
    public void RemoveItem_Should_Work_Only_In_Draft()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, UserService.GetUser());
        var item = new DocumentItem(Guid.NewGuid(), 5);
        doc.AddItem(item);

        doc.RemoveItem(item.Id);
        doc.Items.Should().BeEmpty();

        doc.AddItem(item);
        doc.Confirm(UserService.GetUser());
        Action act = () => doc.RemoveItem(item.Id);
        act.Should().Throw<InvalidOperationException>().WithMessage("Only draft document can be modified.");
    }

    // ================= Confirm =================
    [Fact]
    public void Confirm_Should_Work_Correctly()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, UserService.GetUser());
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 5));

        var confirmedBy = UserService.GetUser();
        doc.Confirm(confirmedBy);

        doc.Status.Should().Be(DocumentStatus.Confirmed);
        doc.ConfirmedByUser.Should().Be(confirmedBy);
        doc.ConfirmedAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Confirm_Should_Throw_If_No_Items()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, UserService.GetUser());
        Action act = () => doc.Confirm(UserService.GetUser());
        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot confirm document without items.");
    }

    // ================= Cancel =================
    [Fact]
    public void Cancel_Should_Work_From_Draft()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, UserService.GetUser());
        doc.Cancel();
        doc.Status.Should().Be(DocumentStatus.Cancelled);

        Action act = () => doc.Cancel();
        act.Should().Throw<InvalidOperationException>().WithMessage("Document is already cancelled.");
    }

    [Fact]
    public void Cancel_Should_Throw_For_Confirmed()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, UserService.GetUser());
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 5));
        doc.Confirm(UserService.GetUser());

        Action act = () => doc.Cancel();
        act.Should().Throw<InvalidOperationException>().WithMessage("Confirmed document cannot be cancelled.");
    }

    // ================= Transfer =================
    [Fact]
    public void Draft_Cannot_StartTransfer()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, UserService.GetUser());
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 5));

        var transferUser = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        Action act = () => doc.StartTransfer(transferUser, now);
        act.Should().Throw<InvalidOperationException>().WithMessage("Only confirmed document can be transferred.");
    }

    [Fact]
    public void Cancelled_Cannot_StartTransfer()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, UserService.GetUser());
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
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, UserService.GetUser());
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 5));
        doc.Confirm(UserService.GetUser());

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
        var createdBy = UserService.GetUser();
        var transferUser = Guid.NewGuid();
        var document = new Document(DateTime.Today, DocumentType.PZ, createdBy);
        var item = new DocumentItem(Guid.NewGuid(), 5, null, null, Guid.NewGuid());
        document.AddItem(item);

        // Confirm first to allow transfer
        document.Confirm(createdBy);
        document.StartTransfer(transferUser, DateTimeOffset.UtcNow);

        var confirmedBy = UserService.GetUser();

        // Act
        document.CompleteTransfer(confirmedBy);

        // Assert
        document.Status.Should().Be(DocumentStatus.Confirmed);
        document.ConfirmedByUser.Should().Be(confirmedBy);
        document.ConfirmedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CompleteTransfer_ShouldThrow_WhenDocumentIsNotInTransfer()
    {
        // Arrange
        var createdBy = UserService.GetUser();
        var document = new Document(DateTime.Today, DocumentType.PZ, createdBy);
        var item = new DocumentItem(Guid.NewGuid(), 5, null, null, Guid.NewGuid());
        document.AddItem(item);

        var confirmedBy = UserService.GetUser();

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
    [Fact]
    public void NewDocument_HasNullNumber()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, AnyUser(), Guid.NewGuid());
        doc.Number.Should().BeNull();
    }

    [Fact]
    public void SetNumber_AssignsNumber_WhenValid()
    {
        var doc = DraftWithItem();
        doc.SetNumber("PZ/2024/001");
        doc.Number.Should().Be("PZ/2024/001");
    }

    [Fact]
    public void SetNumber_Throws_WhenEmpty()
    {
        var doc = DraftWithItem();
        var act = () => doc.SetNumber("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetNumber_Throws_WhenExceeds50Chars()
    {
        var doc = DraftWithItem();
        var act = () => doc.SetNumber(new string('X', 51));
        act.Should().Throw<ArgumentException>();
    }

    // --- Lifecycle ---

    [Fact]
    public void Confirm_SetsStatusToConfirmed_WhenDraftWithItems()
    {
        var doc = DraftWithItem();
        doc.SetNumber("PZ/2024/001");
        doc.Confirm(AnyUser());
        doc.Status.Should().Be(DocumentStatus.Confirmed);
    }

    [Fact]
    public void Confirm_Throws_WhenNoItems()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, AnyUser(), Guid.NewGuid());
        doc.SetNumber("PZ/2024/001");
        var act = () => doc.Confirm(AnyUser());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*without items*");
    }

    [Fact]
    public void Confirm_Throws_WhenAlreadyConfirmed()
    {
        var doc = DraftWithItem();
        doc.SetNumber("PZ/2024/001");
        doc.Confirm(AnyUser());

        var act = () => doc.Confirm(AnyUser());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only draft*");
    }

    [Fact]
    public void Cancel_Throws_WhenAlreadyCancelled()
    {
        var doc = DraftWithItem();
        doc.Cancel();
        var act = () => doc.Cancel();
        act.Should().Throw<InvalidOperationException>().WithMessage("*already cancelled*");
    }

    [Fact]
    public void Cancel_Throws_WhenConfirmed()
    {
        var doc = DraftWithItem();
        doc.SetNumber("PZ/2024/001");
        doc.Confirm(AnyUser());
        var act = () => doc.Cancel();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Confirmed document*");
    }

    [Fact]
    public void StartTransfer_Throws_WhenCancelled()
    {
        var doc = DraftWithItem();
        doc.Cancel();
        var act = () => doc.StartTransfer(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Cancelled*");
    }

    // --- Item invariants ---

    [Fact]
    public void ReplaceItems_ReplacesAllItems_NotAppends()
    {
        var doc = DraftWithItem(); // 1 item
        var newItems = new[]
        {
            new DocumentItem(Guid.NewGuid(), 2, null, Guid.NewGuid(), null),
            new DocumentItem(Guid.NewGuid(), 3, null, Guid.NewGuid(), null)
        };

        doc.ReplaceItems(newItems);

        doc.Items.Should().HaveCount(2, "ReplaceItems replaces, not appends");
    }

    [Fact]
    public void ReplaceItems_Throws_WhenListEmpty()
    {
        var doc = DraftWithItem();
        var act = () => doc.ReplaceItems(Array.Empty<DocumentItem>());
        act.Should().Throw<InvalidOperationException>().WithMessage("*at least one item*");
    }

    [Fact]
    public void ReplaceItems_CalledTwice_ReflectsLastReplace()
    {
        var doc = DraftWithItem();
        var first = new[] { new DocumentItem(Guid.NewGuid(), 1, null, Guid.NewGuid(), null) };
        var second = new[] { new DocumentItem(Guid.NewGuid(), 2, null, Guid.NewGuid(), null) };

        doc.ReplaceItems(first);
        doc.ReplaceItems(second);

        doc.Items.Should().HaveCount(1);
        doc.Items.First().Quantity.Should().Be(2);
    }

    [Fact]
    public void AddItem_Throws_WhenDocumentConfirmed()
    {
        var doc = DraftWithItem();
        doc.SetNumber("PZ/2024/001");
        doc.Confirm(AnyUser());

        var act = () => doc.AddItem(new DocumentItem(Guid.NewGuid(), 1, null, Guid.NewGuid(), null));
        act.Should().Throw<InvalidOperationException>().WithMessage("*draft*");
    }

    [Fact]
    public void ChangeDate_Throws_WhenDocumentConfirmed()
    {
        var doc = DraftWithItem();
        doc.SetNumber("PZ/2024/001");
        doc.Confirm(AnyUser());

        var act = () => doc.ChangeDate(DateTime.UtcNow.AddDays(1));
        act.Should().Throw<InvalidOperationException>().WithMessage("*draft*");
    }

    // --- UserSnapshot ---

    [Fact]
    public void CreatedBySnapshot_IsImmutable_AfterCreation()
    {
        var creator = new UserSnapshot(Guid.NewGuid(), "anna@wms.pl", "Anna Nowak");
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, creator, Guid.NewGuid());

        doc.CreatedByUser.Name.Should().Be("Anna Nowak");
        doc.CreatedByUser.Email.Should().Be("anna@wms.pl");
    }

    [Fact]
    public void ConfirmedBySnapshot_IsSet_AfterConfirm()
    {
        var confirmer = new UserSnapshot(Guid.NewGuid(), "piotr@wms.pl", "Piotr Wiśniewski");
        var doc = DraftWithItem();
        doc.SetNumber("PZ/2024/001");
        doc.Confirm(confirmer);

        doc.ConfirmedByUser.Name.Should().Be("Piotr Wiśniewski");
        doc.ConfirmedByUser.Email.Should().Be("piotr@wms.pl");
    }
}