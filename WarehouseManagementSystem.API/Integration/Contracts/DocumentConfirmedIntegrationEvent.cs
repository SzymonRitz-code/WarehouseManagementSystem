namespace WarehouseManagementSystem.API.Integration.Contracts;

/// <summary>
/// Represents an integration event that is published when a document is confirmed in the warehouse management system.
/// </summary>
public class DocumentConfirmedIntegrationEvent
{
    // TODO(RECRUITMENT): Dodaj jawne SchemaVersion/EventVersion. Publisher i consumer moga byc wdrazane
    // niezaleznie i przez pewien czas obslugiwac rozne wersje kontraktu.
    public Guid MessageId { get; init; }
    public Guid CorrelationId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public Guid DocumentId { get; init; }
    public string DocumentNumber { get; init; } = null!;
    public string DocumentType { get; init; } = null!;
    public Guid SourceWarehouseId { get; init; }
    public Guid? TargetWarehouseId { get; init; }
    public DateTimeOffset ConfirmedAt { get; init; }
    public ConfirmedByPayload ConfirmedBy { get; init; } = null!;
    // TODO(RECRUITMENT): Zweryfikuj minimalizacje danych i RODO. Email trafia do brokera, DLQ i backupow;
    // usun go z eventu, jezeli downstream nie potrzebuje tej danej.
    public IReadOnlyList<DocumentConfirmedItemPayload> Items { get; init; } = [];
}

/// <summary>
/// Represents the payload of the user who confirmed the document in the warehouse management system.
/// </summary>
public class ConfirmedByPayload
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Email { get; init; } = null!;
}
/// <summary>
/// Represents the payload of an item in a confirmed document in the warehouse management system.
/// </summary>
public class DocumentConfirmedItemPayload
{
    public Guid ProductId { get; init; }
    public decimal Quantity { get; init; }
    public Guid? ProductBatchId { get; init; }
    public Guid? SourceZoneId { get; init; }
    public Guid? TargetZoneId { get; init; }
}
