using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.API.DTO;

public record struct ProductBatchDto(
    [property: Required] Guid Id,

    [property: Required, StringLength(50)]
    string BatchNumber,

    DateOnly? ExpirationDate,

    DateOnly? ManufacturedDate,

    [property: Required]
    DateTimeOffset CreatedAt,

    [property: Required]
    Guid ProductId,

    [property: Required, StringLength(200)]
    string ProductName
);
