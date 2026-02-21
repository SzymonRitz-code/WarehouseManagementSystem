namespace WarehouseManagementSystem.Domain.Model.InventoryDomain;
public class StockReservation
{
    public Guid Id { get; set; }

    public decimal Quantity { get; set; }
    public string ReservationSource { get; set; }
    public string Status { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }

    public Guid StockId { get; set; }
    public Stock Stock { get; set; }
}
