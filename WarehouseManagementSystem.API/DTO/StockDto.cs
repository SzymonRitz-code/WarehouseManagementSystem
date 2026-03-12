using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.API.DTO;

public record struct StockDto(
    [property: Required] Guid Id,

    [property: StringLength(50)]
    string? ProductBatchNumber,

    [property: Range(0, double.MaxValue)]
    decimal QuantityTotal,

    [property: Range(0, double.MaxValue)]
    decimal QuantityReserved,

    [property: Range(0, double.MaxValue)]
    decimal Available,

    [property: Required]
    DateTimeOffset LastUpdated,

    [property: Required]
    Guid ProductId,

    [property: Required, StringLength(200)]
    string ProductName,

    [property: Required]
    Guid WarehouseId,

    [property: Required, StringLength(200)]
    string WarehouseName,

    [property: Required]
    Guid WarehouseZoneId,

    [property: Required, StringLength(200)]
    string WarehouseZoneName,

    Guid? ProductBatchId
);
