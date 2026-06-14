using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.API.DTO;

public record struct ProductListDto(
    Guid Id,
    string Sku,
    string Name,
    UnitOfMeasure Unit,
    bool RequiresBatch,
    decimal? Weight,
    decimal? Volume,
    bool IsActive);
