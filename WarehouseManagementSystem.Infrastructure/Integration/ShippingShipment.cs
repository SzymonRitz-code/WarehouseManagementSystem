namespace WarehouseManagementSystem.Infrastructure.Integration;

public class ShippingShipment
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentNumber { get; set; } = null!;
    public string DocumentType { get; set; } = null!;
    public Guid SourceWarehouseId { get; set; }
    public Guid? TargetWarehouseId { get; set; }
    public Guid MessageId { get; set; }
    public Guid? CorrelationId { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Status { get; set; } = "Requested";
}
