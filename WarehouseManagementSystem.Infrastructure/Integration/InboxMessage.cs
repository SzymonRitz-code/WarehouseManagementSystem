namespace WarehouseManagementSystem.Infrastructure.Integration;

public sealed class InboxMessage
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string Consumer { get; set; } = null!;
    public string MessageType { get; set; } = null!;
    public Guid CorrelationId { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string Status { get; set; } = null!;
    public string? LastError { get; set; }
    public int RetryCount { get; set; }
}

public sealed class ErpOrderImport
{
    public Guid Id { get; set; }
    public string ExternalOrderId { get; set; } = null!;
    public Guid WmsDocumentId { get; set; }
    public Guid CorrelationId { get; set; }
    public string PayloadFingerprint { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}
