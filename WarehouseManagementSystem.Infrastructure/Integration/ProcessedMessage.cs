namespace WarehouseManagementSystem.Infrastructure.Integration;

public class ProcessedMessage
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string Consumer { get; set; } = null!;
    public string MessageType { get; set; } = null!;
    public Guid? CorrelationId { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }

    // Retention policy: keep deduplication identifiers indefinitely. Arbitrarily old operator replays
    // are supported, so deleting these rows would weaken idempotency. Payloads are not stored here.
    // Shipping also has a permanent unique DocumentId business key. Published outbox payloads expire separately.
}
