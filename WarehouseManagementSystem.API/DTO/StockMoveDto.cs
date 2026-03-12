namespace WarehouseManagementSystem.API.DTO
{
    public record struct StockMoveDto(
        Guid ProductId,
        Guid SourceWarehouseId,
        Guid SourceZoneId,
        Guid TargetWarehouseId,
        Guid TargetZoneId,
        decimal Quantity,
        Guid? ProductBatchId);
}