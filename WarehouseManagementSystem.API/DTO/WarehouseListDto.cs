namespace WarehouseManagementSystem.API.DTO;

public record struct WarehouseListDto(
    Guid Id,
    string Code,
    string Name,
    string Country,
    string Address,
    int ZonesCount,
    int TotalStock,
    decimal TotalQty,
    bool IsActive,
    DateTimeOffset CreatedAt);
