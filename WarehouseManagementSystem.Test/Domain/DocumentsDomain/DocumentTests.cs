using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using WarehouseManagementSystem.API.Services.User;
using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Exceptions;
using WarehouseManagementSystem.Domain.Model.DocumentsDomain;
using WarehouseManagementSystem.Domain.ValueObjects;
using WarehouseManagementSystem.Tests.Support;
using Document = WarehouseManagementSystem.Domain.Model.DocumentsDomain.Document;

namespace WarehouseManagementSystem.Tests.Domain.DocumentsDomain;

/// <summary>
/// Tests for the <see cref="Document"/> class in the Documents domain.
/// </summary>
public class DocumentTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    public DocumentTests()
    {
        _userServiceMock.Setup(s => s.GetUser(It.IsAny<HttpContext>()))
            .Returns(new UserSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Testomir.Testowski@gmail.com", "Testomir"));
    }

    #region Helper Methods

    /// <summary>
    /// Creates a draft document with one item for testing purposes.
    /// </summary>
    /// <returns>A draft <see cref="Document"/> with one item.</returns>
    private Document DraftWithItem()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, _userServiceMock.Object.GetUser(default), Guid.NewGuid());
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 1, null, Guid.NewGuid(), null));
        return doc;
    }

    #endregion

    #region Constructor and Properties Tests

    [Fact]
    public void Constructor_Should_Set_Properties_Correctly()
    {
        var documentDate = DateTime.UtcNow;
        var type = DocumentType.PZ;
        var createdBy = _userServiceMock.Object.GetUser(default);
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

    #endregion

    #region Number and Notes Validation Tests

    [Theory]
    [ClassData(typeof(InvalidRequiredStringTestData))]
    public void SetNumber_Should_Throw_On_Empty(string? invalidNumber)
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, _userServiceMock.Object.GetUser(default));
        Action act = () => doc.SetNumber(invalidNumber!);
        act.Should().Throw<ArgumentException>().WithMessage("Document number cannot be empty.");
    }

    [Fact]
    public void SetNumber_Should_Throw_If_Too_Long()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, _userServiceMock.Object.GetUser(default));
        var longNumber = new string('A', 51);
        Action act = () => doc.SetNumber(longNumber);
        act.Should().Throw<ArgumentException>().WithMessage("Document number cannot exceed 50 characters.");
    }

    [Fact]
    public void SetNotes_Should_Throw_If_Too_Long()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, _userServiceMock.Object.GetUser(default));
        var longNotes = new string('A', 1001);
        Action act = () => doc.SetNotes(longNotes);
        act.Should().Throw<ArgumentException>().WithMessage("Notes cannot exceed 1000 characters.");
    }

    #endregion

    #region Draft Operations Tests

    [Fact]
    public void ChangeDate_Should_Work_Only_In_Draft()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, _userServiceMock.Object.GetUser(default));
        var newDate = DateTime.UtcNow.AddDays(1);

        doc.ChangeDate(newDate);
        doc.DocumentDate.Should().Be(newDate);

        doc.AddItem(new DocumentItem(Guid.NewGuid(), 1));
        doc.Confirm(_userServiceMock.Object.GetUser(default));

        Action act = () => doc.ChangeDate(DateTime.UtcNow.AddDays(2));
        act.Should().Throw<DocumentNotInDraftStateException>().WithMessage($"Document {doc.Id} is not in Draft state.");
    }

    [Fact]
    public void AddItem_Should_Work_Only_In_Draft()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, _userServiceMock.Object.GetUser(default));
        var item = new DocumentItem(Guid.NewGuid(), 5);

        doc.AddItem(item);
        doc.Items.Should().ContainSingle().Which.Should().Be(item);

        doc.Confirm(_userServiceMock.Object.GetUser(default));
        Action act = () => doc.AddItem(new DocumentItem(Guid.NewGuid(), 1));
        act.Should().Throw<DocumentNotInDraftStateException>().WithMessage($"Document {doc.Id} is not in Draft state.");
    }

    [Fact]
    public void RemoveItem_Should_Work_Only_In_Draft()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, _userServiceMock.Object.GetUser(default));
        var item = new DocumentItem(Guid.NewGuid(), 5);
        doc.AddItem(item);

        doc.RemoveItem(item.Id);
        doc.Items.Should().BeEmpty();

        doc.AddItem(item);
        doc.Confirm(_userServiceMock.Object.GetUser(default));
        Action act = () => doc.RemoveItem(item.Id);
        act.Should().Throw<DocumentNotInDraftStateException>().WithMessage($"Document {doc.Id} is not in Draft state.");
    }

    #endregion

    #region Confirm Operation Tests

    [Fact]
    public void Confirm_Should_Work_Correctly()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, _userServiceMock.Object.GetUser(default));
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 5));

        var confirmedBy = _userServiceMock.Object.GetUser(default);
        doc.Confirm(confirmedBy);

        doc.Status.Should().Be(DocumentStatus.Confirmed);
        doc.ConfirmedByUser.Should().Be(confirmedBy);
        doc.ConfirmedAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Confirm_Should_Throw_If_No_Items()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, _userServiceMock.Object.GetUser(default));
        Action act = () => doc.Confirm(_userServiceMock.Object.GetUser(default));
        act.Should().Throw<CannotConfirmEmptyDocumentException>().WithMessage($"Document {doc.Id} cannot be confirmed without items.");
    }

    #endregion

    #region Cancel Operation Tests

    [Fact]
    public void Cancel_Should_Work_From_Draft()
    {
        var user = _userServiceMock.Object.GetUser(default);
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, user);
        doc.Cancel(user);
        doc.Status.Should().Be(DocumentStatus.Cancelled);

        Action act = () => doc.Cancel(user);
        act.Should().Throw<DocumentAlreadyCancelledException>().WithMessage($"Document {doc.Id} is already cancelled.");
    }

    [Fact]
    public void Cancel_Should_Throw_For_Confirmed()
    {
        var user = _userServiceMock.Object.GetUser(default);
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, user);
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 5));
        doc.Confirm(_userServiceMock.Object.GetUser(default));

        Action act = () => doc.Cancel(user);
        act.Should().Throw<DocumentNotInDraftStateException>().WithMessage($"Document {doc.Id} is not in Draft state.");
    }

    #endregion

    #region Transfer Operation Tests

    [Fact]
    public void Draft_Cannot_StartTransfer()
    {
        var user = _userServiceMock.Object.GetUser(default);
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, user);
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 5));

        var transferUser = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        Action act = () => doc.StartTransfer(now);
        act.Should().Throw<DocumentNotInDraftStateException>().WithMessage($"Document {doc.Id} is not in Draft state.");
    }

    [Fact]
    public void Cancelled_Cannot_StartTransfer()
    {
        var user = _userServiceMock.Object.GetUser(default);
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, user);
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 5));
        doc.Cancel(user);

        var transferUser = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        Action act = () => doc.StartTransfer(now);
        act.Should().Throw<DocumentNotInDraftStateException>().WithMessage($"Document {doc.Id} is not in Draft state.");
    }

    [Fact]
    public void Confirmed_Can_StartTransfer()
    {
        var user = _userServiceMock.Object.GetUser(default);
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, user);
        doc.AddItem(new DocumentItem(Guid.NewGuid(), 5));
        doc.Confirm(user);

        var transferUser = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        doc.StartTransfer(now);

        doc.Status.Should().Be(DocumentStatus.Transfer);
        doc.TransferStartedAt.Should().Be(now);
    }

    [Fact]
    public void CompleteTransfer_ShouldSetStatusToConfirmed_WhenDocumentIsInTransfer()
    {
        // Arrange
        var createdBy = _userServiceMock.Object.GetUser(default);
        var document = new Document(DateTime.Today, DocumentType.PZ, createdBy);
        var item = new DocumentItem(Guid.NewGuid(), 5, null, null, Guid.NewGuid());
        document.AddItem(item);

        // Confirm first to allow transfer
        document.Confirm(createdBy);
        document.StartTransfer(DateTimeOffset.UtcNow);

        var confirmedBy = _userServiceMock.Object.GetUser(default);

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
        var createdBy = _userServiceMock.Object.GetUser(default);
        var document = new Document(DateTime.Today, DocumentType.PZ, createdBy);
        var item = new DocumentItem(Guid.NewGuid(), 5, null, null, Guid.NewGuid());
        document.AddItem(item);

        var confirmedBy = _userServiceMock.Object.GetUser(default);

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

    #endregion

    #region Number Lifecycle Tests

    [Fact]
    public void NewDocument_HasNullNumber()
    {
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, _userServiceMock.Object.GetUser(default), Guid.NewGuid());
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

    #endregion

    #region Document Lifecycle Tests

    [Fact]
    public void Confirm_SetsStatusToConfirmed_WhenDraftWithItems()
    {
        var doc = DraftWithItem();
        doc.SetNumber("PZ/2024/001");
        doc.Confirm(_userServiceMock.Object.GetUser(default));
        doc.Status.Should().Be(DocumentStatus.Confirmed);
    }

    [Fact]
    public void Confirm_Throws_WhenNoItems()
    {
        var user = _userServiceMock.Object.GetUser(default);
        var doc = new Document(DateTime.UtcNow, DocumentType.PZ, user, Guid.NewGuid());
        doc.SetNumber("PZ/2024/001");
        var act = () => doc.Confirm(user);
        act.Should().Throw<CannotConfirmEmptyDocumentException>()
            .WithMessage($"Document {doc.Id} cannot be confirmed without items.");
    }

    [Fact]
    public void Confirm_Throws_WhenAlreadyConfirmed()
    {
        var user = _userServiceMock.Object.GetUser(default);
        var doc = DraftWithItem();
        doc.SetNumber("PZ/2024/001");
        doc.Confirm(user);

        var act = () => doc.Confirm(user);
        act.Should().Throw<DocumentNotInDraftStateException>()
            .WithMessage($"Document {doc.Id} is not in Draft state.");
    }

    [Fact]
    public void Cancel_Throws_WhenAlreadyCancelled()
    {
        var user = _userServiceMock.Object.GetUser(default);
        var doc = DraftWithItem();
        doc.Cancel(user);
        var act = () => doc.Cancel(user);
        act.Should().Throw<DocumentAlreadyCancelledException>().WithMessage($"Document {doc.Id} is already cancelled.");
    }

    [Fact]
    public void Cancel_Throws_WhenConfirmed()
    {
        var confirmedby = _userServiceMock.Object.GetUser(default);
        var cancelledby = _userServiceMock.Object.GetUser(default);
        var doc = DraftWithItem();
        doc.SetNumber("PZ/2024/001");
        doc.Confirm(confirmedby);
        var act = () => doc.Cancel(cancelledby);
        act.Should().Throw<DocumentNotInDraftStateException>().WithMessage($"Document {doc.Id} is not in Draft state.");
    }

    [Fact]
    public void StartTransfer_Throws_WhenCancelled()
    {
        var doc = DraftWithItem();
        doc.Cancel(_userServiceMock.Object.GetUser(default));
        var act = () => doc.StartTransfer(DateTimeOffset.UtcNow);
        act.Should().Throw<DocumentNotInDraftStateException>().WithMessage($"Document {doc.Id} is not in Draft state.");
    }

    #endregion

    #region Item Invariants Tests

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
        act.Should().Throw<InvalidOperationException>().WithMessage($"Document must have at least one item.");
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
        doc.Confirm(_userServiceMock.Object.GetUser(default));

        var act = () => doc.AddItem(new DocumentItem(Guid.NewGuid(), 1, null, Guid.NewGuid(), null));
        act.Should().Throw<DocumentNotInDraftStateException>().WithMessage($"Document {doc.Id} is not in Draft state.");
    }

    [Fact]
    public void ChangeDate_Throws_WhenDocumentConfirmed()
    {
        var doc = DraftWithItem();
        doc.SetNumber("PZ/2024/001");
        doc.Confirm(_userServiceMock.Object.GetUser(default));

        var act = () => doc.ChangeDate(DateTime.UtcNow.AddDays(1));
        act.Should().Throw<DocumentNotInDraftStateException>().WithMessage($"Document {doc.Id} is not in Draft state.");
    }

    #endregion

    #region UserSnapshot Tests

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

    #endregion
}
