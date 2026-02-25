namespace WarehouseManagementSystem.API.DTO
{
    public record struct StockReservationRequestDto(
        Guid StockId,
        decimal Quantity,
        string ReservationSource,
        Guid CreatedBy,
        DateTimeOffset? ExpiresAt = null);
}