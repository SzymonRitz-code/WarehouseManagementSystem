namespace WarehouseManagementSystem.Infrastructure.Integration;

public class ProcessedMessage
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string Consumer { get; set; } = null!;
    public string MessageType { get; set; } = null!;
    public Guid? CorrelationId { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}
