namespace WarehouseManagementSystem.API.DTO
{
    // ===== SUPPORTING DTOs FOR COMMANDS =====
    public record struct StockChangeDto(
        Guid ProductId,
        Guid WarehouseId,
        Guid WarehouseZoneId,
        decimal Quantity,
        Guid? ProductBatchId);
}