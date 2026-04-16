 namespace WarehouseManagementSystem.API.DTO;

public record struct ProductBatchListDto
(
    Guid Id,
    string BatchNumber,
    string ProductName,
    DateOnly? ExpirationDate,
    int Quantity,
    int AvailableQty,
    int ReservedQty,
    DateTimeOffset CreatedAt
);

