namespace WarehouseManagementSystem.Infrastructure.Integration;

/// <summary>
/// Represents a message in the outbox for integration purposes.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string Type { get; set; } = null!;
    public string RoutingKey { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public Guid? CorrelationId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string Status { get; set; } = OutboxMessageStatus.Pending;
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Represents the possible statuses of an outbox message.
/// </summary>
public static class OutboxMessageStatus
{
    public const string Pending = "Pending";
    public const string Published = "Published";
    public const string Failed = "Failed";
}
