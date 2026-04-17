namespace WarehouseManagementSystem.API.DTO
{
    public record struct StockMoveDto
    (
    Guid Id,
    Guid ProductId,
    Guid ProductBatchId,
    string ProductSku,
    string ProductName,
    Guid SourceWarehouseId,
    string SourceWarehouse,
    Guid SourceZoneId,
    string SourceZone,
    Guid TargetWarehouseId,
    string TargetWarehouse,
    Guid TargetZoneId,
    string TargetZone,
    decimal Quantity,
    string Unit,
    string MoveType,
    string Status,
    string MovedBy,
    DateTimeOffset MovedAt,
    string? Reference
    );
}