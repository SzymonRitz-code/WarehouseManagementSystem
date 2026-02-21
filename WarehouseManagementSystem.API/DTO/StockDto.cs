namespace WarehouseManagementSystem.API.DTO
{
    public record struct StockDto(
        Guid Id,
        string? ProductBatchNumber,
        decimal QuantityTotal,
        decimal QuantityReserved,
        decimal Available,
        DateTimeOffset LastUpdated,
        Guid ProductId,
        string ProductName,
        Guid WarehouseId,
        string WarehouseName,
        Guid WarehouseZoneId,
        string WarehouseZoneName,
        Guid? ProductBatchId 
    );
}
