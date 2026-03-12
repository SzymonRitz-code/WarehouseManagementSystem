using System.ComponentModel.DataAnnotations;
using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.API.DTO;

public record struct ProductDto(
    [property: Required] Guid Id,

    [property: Required, StringLength(50)]
    string SKU,

    [property: Required, StringLength(200)]
    string Name,

    string? Description,

    [property: Required]
    UnitOfMeasure Unit,

    [property: Required]
    bool RequiresBatch,

    [property: Required]
    bool IsActive,

    [property: Range(0, double.MaxValue)]
    decimal? Weight,

    [property: Range(0, double.MaxValue)]
    decimal? Volume,

    [property: Required]
    DateTimeOffset CreatedAt
);
