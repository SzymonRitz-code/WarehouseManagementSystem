using WarehouseManagementSystem.Domain.Enums;
using WarehouseManagementSystem.Domain.Events;
using WarehouseManagementSystem.Domain.Exceptions;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;
using WarehouseManagementSystem.Domain.ValueObjects;

namespace WarehouseManagementSystem.Domain.Model.DocumentsDomain;

/// <summary>
/// AGGREGATE ROOT: Document represents a warehouse document (e.g., PurchaseOrder, StockTransfer, Sale).
/// 
/// ARCHITECTURAL PATTERN: WMS uses Event-Driven CQRS Architecture.
/// This class exemplifies:
/// 
/// 1. AGGREGATE PATTERN (DDD)
///    - Document is an AGGREGATE ROOT that encapsulates DocumentItems and related data
///    - All invariants (business rules) are enforced within this aggregate
///    - External code should NOT access DocumentItems directly; must go through Document methods
///    - This ensures CONSISTENCY BOUNDARY: if Document is valid, all its related data is valid
/// 
/// 2. ENCAPSULATION OF BUSINESS LOGIC
///    - Operations like Confirm(), Cancel(), StartTransfer() enforce business rules
///    - Status machine pattern: Draft → Confirmed → Transfer → Completed (or Cancelled)
///    - Private setters ensure only aggregate methods change state
///    - Example: Cannot confirm an empty document, cannot cancel already-cancelled document
/// 
/// 3. EVENT-DRIVEN ARCHITECTURE
///    - When aggregate state changes significantly (e.g., Confirm(), Cancel()), domain events are raised
///    - These events are published ASYNCHRONOUSLY by the application layer
///    - Events decouple domain from side effects (e.g., notify warehouse staff, update inventory)
///    - COMPARISON WITH DDD-Fundamentals: 
///      - Here: Events added manually via AddDomainEvent() in Application Service
///      - DDD-Fundamentals: Events added in aggregate, auto-published by DbContext
/// 
/// 4. VALUE OBJECTS
///    - UserSnapshot: Immutable snapshot of user at time of action
///    - DocumentType, DocumentStatus: Enums (could be value objects too)
///    - These prevent temporal data issues (e.g., user name changes after document creation)
/// 
/// COMPARISON WITH DDD-Fundamentals (ClinicManagement):
/// 
/// DDD-Fundamentals approach (BaseEntity<TId>, IAggregateRoot):
/// ┌─────────────────────────────────────────────────────┐
/// │ public class Room : BaseEntity<int>, IAggregateRoot │ ← Explicit IAggregateRoot marker
/// │ {                                                   │
/// │     public string Name { get; set; }               │ ← Inherits Events from BaseEntity
/// │     public List<BaseDomainEvent> Events = new();  │ ← Automatic event tracking
/// │ }                                                   │
/// │                                                     │ ← Events auto-published in SaveChangesAsync()
/// │ PROS: Cleaner code, automatic event publishing    │
/// │ CONS: Less explicit, harder to see event logic    │
/// └─────────────────────────────────────────────────────┘
/// 
/// WMS approach (Document, Implicit aggregate):
/// ┌──────────────────────────────────────────────────────┐
/// │ public class Document                               │ ← No IAggregateRoot (convention-based)
/// │ {                                                    │
/// │     public void Confirm(UserSnapshot confirmedBy)   │ ← Explicit business logic
/// │     {                                               │
/// │         Status = DocumentStatus.Confirmed;         │ ← State change visible
/// │         // AddDomainEvent(new ...) - manual here   │ ← Event must be added manually
/// │     }                                               │
/// │ }                                                    │
/// │                                                     │ ← Events published in CommandService
/// │ PROS: Explicit control, visible intent            │
/// │ CONS: More code, easy to forget event adding       │
/// └──────────────────────────────────────────────────────┘
/// 
/// KEY CONCEPTS FOR LEARNING:
/// - Aggregate: Group of objects treated as single unit (Document + DocumentItems)
/// - Invariant: A rule that must ALWAYS be true (e.g., "non-empty document can be confirmed")
/// - Boundary: An aggregate is a transaction boundary (save whole aggregate or nothing)
/// - Repository: Talk to aggregates through repository, never directly (hidden behind IDocumentRepository)
/// - Event: Immutable record of something that happened (DocumentConfirmedEvent)
/// </summary>
public class Document : IHasDomainEvents
{
    #region Fields and Constructors

    private const int MaxNumberLength = 50;
    private const int MaxNotesLength = 1000;

    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// PRIVATE collection ensures external code CANNOT directly add/remove items.
    /// Must use public methods (AddItem, RemoveItem, ReplaceItems) which enforce business rules.
    /// 
    /// This is ENCAPSULATION: hiding internal implementation details and enforcing invariants.
    /// </summary>
    private readonly List<DocumentItem> _items = new();

    /// <summary>
    /// Private constructor for EF Core only.
    /// All document creation should use the public constructor below.
    /// This prevents accidental invalid state creation.
    /// </summary>
    private Document() { } // EF

    /// <summary>
    /// FACTORY METHOD: Creates a new document with initial state.
    /// 
    /// Business Rules Enforced Here:
    /// - Document starts in Draft status (not yet confirmed)
    /// - CreatedBy user is captured (via UserSnapshot value object)
    /// - Timestamps are set (CreatedAt)
    /// - Both warehouses can be null initially (set later via SetSourceWarehouse/SetTargetWarehouse)
    /// 
    /// COMPARISON: DDD-Fundamentals would use static Room.Create() similarly.
    /// </summary>
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
        Status = DocumentStatus.Draft;  // Always start in Draft
        CreatedByUser = createdByUser;
        SourceWarehouseId = sourceWarehouseId;
        TargetWarehouseId = targetWarehouseId;
        SetNotes(notes);  // Validates length
        CreatedAt = DateTimeOffset.UtcNow;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Unique identifier for this document aggregate.
    /// Private setter ensures only constructor/EF can set this.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Human-readable document number (e.g., "DOC-2024-001").
    /// Can be null initially, set via SetNumber() after generation.
    /// Private setter ensures only SetNumber() method can change this.
    /// </summary>
    public string? Number { get; private set; }

    public DateTime DocumentDate { get; private set; }
    public DocumentType Type { get; private set; }

    /// <summary>
    /// STATUS STATE MACHINE:
    /// Draft → Confirmed → Transfer → Completed
    ///     ↓
    ///     └─→ Cancelled (from Draft or Transfer)
    /// 
    /// This is the AGGREGATE STATE that controls what operations are valid.
    /// Private setter ensures only aggregate methods can change status.
    /// </summary>
    public DocumentStatus Status { get; private set; }

    /// <summary>
    /// Optimistic concurrency token (SQL Server ROWVERSION).
    /// Updated by EF when document changes, used to detect conflicts.
    /// In event-driven systems: helps ensure exactly-once processing.
    /// </summary>
    public byte[] RowVersion { get; private set; }

    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Timestamp when document was confirmed (Draft → Confirmed).
    /// Null = not yet confirmed. Combined with ConfirmedByUser creates audit trail.
    /// </summary>
    public DateTimeOffset? ConfirmedAt { get; private set; }

    public DateTimeOffset? TransferStartedAt { get; private set; }

    /// <summary>
    /// Timestamp when document was cancelled.
    /// Null = not cancelled. Used for soft-delete audit trail.
    /// </summary>
    public DateTimeOffset? CancelledAt { get; set; }

    /// <summary>
    /// VALUE OBJECT: Immutable snapshot of user who created this document.
    /// Stored with document to preserve "who created this" even if user later changes name/deleted.
    /// This prevents temporal data anomalies.
    /// </summary>
    public UserSnapshot CreatedByUser { get; private set; }

    /// <summary>
    /// Captured when document transitions to Confirmed status.
    /// Null = not yet confirmed (still in Draft).
    /// </summary>
    public UserSnapshot? ConfirmedByUser { get; private set; }

    public UserSnapshot? CancelledByUser { get; private set; }

    public Guid? SourceWarehouseId { get; private set; }
    public Warehouse? SourceWarehouse { get; private set; }

    public Guid? TargetWarehouseId { get; private set; }
    public Warehouse? TargetWarehouse { get; private set; }

    /// <summary>
    /// INVARIANT ENFORCEMENT: Return read-only collection.
    /// External code can iterate items but cannot modify them directly.
    /// Must use AddItem(), RemoveItem(), ReplaceItems() methods.
    /// 
    /// This ensures business rules (e.g., non-empty check in Confirm) are always enforced.
    /// </summary>
    public IReadOnlyCollection<DocumentItem> Items => _items.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    #endregion

    #region Domain Event Helpers

    private void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    #endregion

    #region Metadata Operations

    /// <summary>
    /// INVARIANT ENFORCEMENT: Validates and sets document number.
    /// Private setter on Number property ensures only this method can change it.
    /// 
    /// Business Rules:
    /// - Number must not be empty
    /// - Number must not exceed MaxNumberLength
    /// 
    /// Example: User calls document.SetNumber("PO-2024-12345")
    ///          → Validation happens → Number property updated
    ///          → If invalid, InvalidOperationException thrown
    /// </summary>
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

    /// <summary>
    /// INVARIANT ENFORCEMENT: Validates and sets notes.
    /// Similar pattern to SetNumber() - all state changes go through business-logic methods.
    /// </summary>
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

    /// <summary>
    /// INVARIANT ENFORCEMENT: Only modify items while in Draft status.
    /// Once confirmed, document is "locked" - cannot add/remove items.
    /// </summary>
    public void ChangeDate(DateTime newDate)
    {
        EnsureDraft();
        DocumentDate = newDate;
    }

    /// <summary>
    /// AGGREGATE BOUNDARY: All item modifications go through document.
    /// 
    /// Business Rules Enforced:
    /// - Can only add items in Draft status
    /// - Item cannot be null
    /// 
    /// Why not external code doing _items.Add(item)?
    /// Because we can't enforce invariants! Code could:
    /// - Add item to confirmed document (violates state machine)
    /// - Add null item (violates non-null constraint)
    /// 
    /// ENCAPSULATION ensures aggregate integrity.
    /// </summary>
    public void AddItem(DocumentItem item)
    {
        EnsureDraft();

        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        _items.Add(item);
    }

    /// <summary>
    /// BULK OPERATION: Replace all items at once.
    /// Useful for UI workflows where user can modify entire list then save once.
    /// 
    /// Business Rules Enforced:
    /// - Document must be in Draft
    /// - New collection must have at least one item (non-empty invariant)
    /// </summary>
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

    /// <summary>
    /// Remove specific item by ID.
    /// Aggregate boundary ensures we validate before removing.
    /// </summary>
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

    /// <summary>
    /// STATE TRANSITION: Confirmed → Transfer
    /// 
    /// Business Rules:
    /// - Only Confirmed documents can start transfer
    /// - Cancelled documents cannot be transferred
    /// 
    /// EVENT-DRIVEN ARCHITECTURE:
    /// When this method is called from CommandService:
    /// 1. Aggregate state changes (Status = Transfer)
    /// 2. Application layer publishes event: DocumentTransferStartedEvent
    /// 3. Event handlers execute asynchronously (e.g., notify warehouse staff)
    /// 
    /// EXAMPLE Flow:
    /// CommandService.StartTransferAsync(documentId):
    ///   var doc = await repo.GetAsync(documentId)
    ///   doc.StartTransfer(now)  ← This method
    ///   await repo.SaveAsync()  ← Triggers event publishing
    ///   await eventPublisher.PublishAsync(event)  ← Side effects here
    /// </summary>
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

    /// <summary>
    /// STATE TRANSITION: Draft → Confirmed
    /// 
    /// CRITICAL BUSINESS RULE: "Document must have at least one item to be confirmed"
    /// This INVARIANT is enforced HERE, in the domain.
    /// Application layer cannot bypass this check.
    /// 
    /// COMPARISON WITH DDD-Fundamentals:
    /// - DDD-Fundamentals: Same logic, but events auto-tracked in BaseEntity.Events
    /// - WMS: Same logic, but event added manually in CommandService
    ///
    /// EXAMPLE:
    /// var document = new Document(...)
    /// document.AddItem(item1)  // Now _items.Count == 1
    /// document.Confirm(user)   // ✅ Allowed
    /// 
    /// var emptyDoc = new Document(...)
    /// document.Confirm(user)   // ❌ Throws CannotConfirmEmptyDocumentException
    /// </summary>
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

        AddDomainEvent(new DocumentConfirmedDomainEvent(
            documentId: Id,
            documentNumber: Number ?? string.Empty,
            documentType: Type.ToString(),
            sourceWarehouseId: SourceWarehouseId ?? Guid.Empty,
            targetWarehouseId: TargetWarehouseId,
            confirmedBy: confirmedByUser,
            occurredAt: ConfirmedAt.Value));
    }

    /// <summary>
    /// STATE TRANSITION: Transfer → Completed
    /// Marks transfer as complete.
    /// </summary>
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

    /// <summary>
    /// STATE TRANSITION: Draft → Cancelled (or Transfer → Cancelled)
    /// 
    /// Business Rules:
    /// - Cannot cancel already-cancelled document (idempotency check)
    /// - Cannot cancel confirmed document (too late, already started business process)
    /// 
    /// IDEMPOTENCY: If someone calls Cancel twice, second call fails with clear error.
    /// Better than silently ignoring or corrupting state.
    /// </summary>
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

        AddDomainEvent(new DocumentCancelledDomainEvent(
            documentId: Id,
            cancelledBy: cancelledByUser,
            occurredAt: CancelledAt.Value));
    }

    #endregion

    #region Warehouse and Type Operations

    /// <summary>
    /// HELPER METHOD: Enforces "only in Draft" invariant for multiple operations.
    /// DRY principle: Instead of repeating if-check in every method, extracted to EnsureDraft().
    /// 
    /// Throws: DocumentNotInDraftStateException
    /// </summary>
    private void EnsureDraft()
    {
        if (Status != DocumentStatus.Draft)
        {
            throw new DocumentNotInDraftStateException(Id);
        }
    }

    /// <summary>
    /// Set source warehouse. Only allowed in Draft status.
    /// After confirmation, warehouse cannot be changed (prevents fraud/errors).
    /// </summary>
    public void SetSourceWarehouse(Guid sourceWarehouseId)
    {
        EnsureDraft();
        SourceWarehouseId = sourceWarehouseId;
    }

    /// <summary>
    /// Set target warehouse (nullable - some document types don't have target).
    /// Only allowed in Draft status.
    /// </summary>
    public void SetTargetWarehouse(Guid? targetWarehouseId)
    {
        EnsureDraft();
        TargetWarehouseId = targetWarehouseId;
    }

    /// <summary>
    /// Set document type. Only allowed in Draft status.
    /// Type determines business rules (e.g., PurchaseOrder vs StockTransfer have different flows).
    /// </summary>
    public void SetDocumentType(DocumentType type)
    {
        EnsureDraft();
        Type = type;
    }

    #endregion
}
