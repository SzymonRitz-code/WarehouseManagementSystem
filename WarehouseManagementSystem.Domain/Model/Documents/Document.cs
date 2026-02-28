using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Model.SecurityDomain;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

namespace WarehouseManagementSystem.Domain.Model.DocumentsDomain;

public class Document
{
    private const int MaxNumberLength = 50;
    private const int MaxNotesLength = 1000;

    private readonly List<DocumentItem> _items = new();

    private Document() { } // EF

    public Document(
        string number,
        DateTime documentDate,
        DocumentType type,
        Guid createdById,
        Guid? sourceWarehouseId = null,
        Guid? targetWarehouseId = null,
        string? notes = null)
    {
        Id = Guid.NewGuid();
        SetNumber(number);
        DocumentDate = documentDate;
        Type = type;
        Status = DocumentStatus.Draft;
        CreatedById = createdById;
        SourceWarehouseId = sourceWarehouseId;
        TargetWarehouseId = targetWarehouseId;
        SetNotes(notes);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Number { get; private set; }
    public DateTime DocumentDate { get; private set; }
    public DocumentType Type { get; private set; }
    public DocumentStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; }

    public string? Notes { get; private set; }
    public DateTimeOffset? CreatedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? TransferStartedAt { get; private set; }


    public Guid CreatedById { get; private set; }
    public User CreatedBy { get; private set; }

    public Guid? ConfirmedById { get; private set; }
    public User? ConfirmedBy { get; private set; }

    public Guid? TransferStartedById { get; private set; }
    public User? TransferStartedBy { get; private set; }

    public Guid? SourceWarehouseId { get; private set; }
    public Warehouse? SourceWarehouse { get; private set; }

    public Guid? TargetWarehouseId { get; private set; }
    public Warehouse? TargetWarehouse { get; private set; }

    public IReadOnlyCollection<DocumentItem> Items => _items.AsReadOnly();

    // ===== Business Methods =====

    public void SetNumber(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Document number cannot be empty.");

        if (number.Length > MaxNumberLength)
            throw new ArgumentException($"Document number cannot exceed {MaxNumberLength} characters.");

        Number = number;
    }

    public void SetNotes(string? notes)
    {
        if (notes != null && notes.Length > MaxNotesLength)
            throw new ArgumentException($"Notes cannot exceed {MaxNotesLength} characters.");

        Notes = notes;
    }

    public void ChangeDate(DateTime newDate)
    {
        EnsureDraft();
        DocumentDate = newDate;
    }

    public void AddItem(DocumentItem item)
    {
        EnsureDraft();

        if (item == null)
            throw new ArgumentNullException(nameof(item));

        _items.Add(item);
    }

    public void RemoveItem(Guid itemId)
    {
        EnsureDraft();

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            throw new InvalidOperationException("Item not found.");

        _items.Remove(item);
    }
    public void StartTransfer(Guid userId, DateTimeOffset now)
    {
        if (Status != DocumentStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed document can be transferred.");

        Status = DocumentStatus.Transfer;
        TransferStartedAt = now;
        TransferStartedById = userId;
    }
    public void Confirm(Guid confirmedById)
    {
        if (!_items.Any())
            throw new InvalidOperationException("Cannot confirm document without items.");

        if (Status != DocumentStatus.Draft)
            throw new InvalidOperationException("Only draft document can be confirmed.");

        if (Status != DocumentStatus.Transfer)
            throw new InvalidOperationException("Only transferred document can be completed.");

        Status = DocumentStatus.Confirmed;
        ConfirmedById = confirmedById;
        ConfirmedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == DocumentStatus.Cancelled)
            throw new InvalidOperationException("Document is already cancelled.");

        if (Status == DocumentStatus.Confirmed)
            throw new InvalidOperationException("Confirmed document cannot be cancelled.");

        Status = DocumentStatus.Cancelled;
    }

    private void EnsureDraft()
    {
        if (Status != DocumentStatus.Draft)
            throw new InvalidOperationException("Only draft document can be modified.");
    }
}
