using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Exceptions;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Domain.Model.DocumentsDomain;

public class Document
{
    #region Fields and Constructors

    private const int MaxNumberLength = 50;
    private const int MaxNotesLength = 1000;

    private readonly List<DocumentItem> _items = new();

    private Document() { } // EF

    public Document(
        DateTime documentDate,
        DocumentType type,
        UserSnapshot createdByUser,
        Guid? sourceWarehouseId = null,
        Guid? targetWarehouseId = null,
        string? notes = null)
    {
        Id = Guid.NewGuid();
        DocumentDate = documentDate;
        Type = type;
        Status = DocumentStatus.Draft;
        CreatedByUser = createdByUser;
        SourceWarehouseId = sourceWarehouseId;
        TargetWarehouseId = targetWarehouseId;
        SetNotes(notes);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    #endregion

    #region Properties

    public Guid Id { get; private set; }
    public string? Number { get; private set; }
    public DateTime DocumentDate { get; private set; }
    public DocumentType Type { get; private set; }
    public DocumentStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; }

    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? TransferStartedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; set; }

    public UserSnapshot CreatedByUser { get; private set; }
    public UserSnapshot? ConfirmedByUser { get; private set; }
    public UserSnapshot? CancelledByUser { get; private set; }

    public Guid? SourceWarehouseId { get; private set; }
    public Warehouse? SourceWarehouse { get; private set; }

    public Guid? TargetWarehouseId { get; private set; }
    public Warehouse? TargetWarehouse { get; private set; }

    public IReadOnlyCollection<DocumentItem> Items => _items.AsReadOnly();

    #endregion

    #region Metadata Operations

    public void SetNumber(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new ArgumentException("Document number cannot be empty.");
        }

        if (number.Length > MaxNumberLength)
        {
            throw new ArgumentException($"Document number cannot exceed {MaxNumberLength} characters.");
        }

        Number = number;
    }

    public void SetNotes(string? notes)
    {
        if (notes != null && notes.Length > MaxNotesLength)
        {
            throw new ArgumentException($"Notes cannot exceed {MaxNotesLength} characters.");
        }

        Notes = notes;
    }

    #endregion

    #region Draft Item Operations

    public void ChangeDate(DateTime newDate)
    {
        EnsureDraft();
        DocumentDate = newDate;
    }

    public void AddItem(DocumentItem item)
    {
        EnsureDraft();

        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        _items.Add(item);
    }
    public void ReplaceItems(IEnumerable<DocumentItem> newItems)
    {
        EnsureDraft();
        if (!newItems.Any())
        {
            throw new InvalidOperationException("Document must have at least one item.");
        }

        _items.Clear();
        foreach (var item in newItems)
        {
            _items.Add(item);
        }
    }
    public void RemoveItem(Guid itemId)
    {
        EnsureDraft();

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
        {
            throw new InvalidOperationException("Item not found.");
        }

        _items.Remove(item);
    }

    #endregion

    #region Workflow Operations

    public void StartTransfer(DateTimeOffset now)
    {
        if (Status == DocumentStatus.Cancelled)
        {
            throw new DocumentNotInDraftStateException(Id);
        }

        if (Status != DocumentStatus.Confirmed)
        {
            throw new DocumentNotInDraftStateException(Id);
        }

        Status = DocumentStatus.Transfer;
        TransferStartedAt = now;
    }

    public void Confirm(ValueObjects.UserSnapshot confirmedByUser)
    {
        if (!_items.Any())
        {
            throw new CannotConfirmEmptyDocumentException(Id);
        }

        if (Status != DocumentStatus.Draft)
        {
            throw new DocumentNotInDraftStateException(Id);
        }

        Status = DocumentStatus.Confirmed;
        ConfirmedByUser = confirmedByUser;
        ConfirmedAt = DateTimeOffset.UtcNow;
    }

    public void CompleteTransfer(ValueObjects.UserSnapshot confirmedByUser)
    {
        if (Status != DocumentStatus.Transfer)
        {
            throw new InvalidOperationException("Only transferred document can be completed.");
        }

        Status = DocumentStatus.Confirmed;
        ConfirmedByUser = confirmedByUser;
        ConfirmedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel(UserSnapshot cancelledByUser)
    {
        if (Status == DocumentStatus.Cancelled)
        {
            throw new DocumentAlreadyCancelledException(Id);
        }

        if (Status == DocumentStatus.Confirmed)
        {
            throw new DocumentNotInDraftStateException(Id);
        }

        Status = DocumentStatus.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;
        CancelledByUser = cancelledByUser;
    }

    #endregion

    #region Warehouse and Type Operations

    private void EnsureDraft()
    {
        if (Status != DocumentStatus.Draft)
        {
            throw new DocumentNotInDraftStateException(Id);
        }
    }

    public void SetSourceWarehouse(Guid sourceWarehouseId)
    {
        EnsureDraft();
        SourceWarehouseId = sourceWarehouseId;
    }

    public void SetTargetWarehouse(Guid? targetWarehouseId)
    {
        EnsureDraft();
        TargetWarehouseId = targetWarehouseId;
    }

    public void SetDocumentType(DocumentType type)
    {
        EnsureDraft();
        Type = type;
    }

    #endregion
}
