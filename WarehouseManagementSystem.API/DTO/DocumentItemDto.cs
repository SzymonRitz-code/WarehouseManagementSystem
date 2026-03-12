 using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.API.DTO;

public record struct DocumentItemDto(
    [property: Required] Guid Id,

    [property: Required]
    Guid DocumentId,

    [property: Required]
    Guid ProductId,

    [property: StringLength(200)]
    string ProductName,

    Guid? ProductBatchId,

    [property: StringLength(50)]
    string? ProductBatchNumber,

    Guid? SourceZoneId,

    [property: StringLength(200)]
    string? SourceZoneName,

    Guid? TargetZoneId,

    [property: StringLength(200)]
    string? TargetZoneName,

    [property: Range(0, double.MaxValue)]
    decimal Quantity
);
