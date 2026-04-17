namespace WarehouseManagementSystem.API.DTO;

public record struct StockDto(
    Guid Id,
    string? ProductBatchNumber,
    decimal QuantityTotal,
    decimal QuantityReserved,
    decimal QuantityAvailable,
    DateTimeOffset LastUpdated,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    Guid WarehouseId,
    string WarehouseName,
    Guid ZoneId,
    string ZoneName,
    string Unit
);
