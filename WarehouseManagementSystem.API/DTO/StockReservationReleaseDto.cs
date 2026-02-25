namespace WarehouseManagementSystem.API.DTO
{
    public record struct StockReservationReleaseDto(
        Guid StockId,
        decimal Quantity);
}