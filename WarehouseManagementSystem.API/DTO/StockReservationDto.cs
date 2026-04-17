 using System.ComponentModel.DataAnnotations;
using WarehouseManagementSystem.Domain.Enums;

namespace WarehouseManagementSystem.API.DTO;

public record struct StockReservationDto(
    [property: Required] Guid Id,

    [property: Range(0, double.MaxValue)]
    decimal Quantity,

    [property: Required]
    ReservationStatus Status,

    DateTimeOffset? ExpiresAt,

    [property: Required]
    DateTimeOffset CreatedAt,

    [property: Required]
    Guid CreatedById,

    [property: Required]
    Guid StockId
);
