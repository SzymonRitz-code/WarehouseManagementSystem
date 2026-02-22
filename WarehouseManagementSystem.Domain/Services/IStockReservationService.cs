using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.Domain.Services;

public interface IStockReservationService
{
    Task<StockReservation> CreateReservationAsync(
        Guid stockId,
        decimal quantity,
        string source,
        Guid createdBy,
        DateTimeOffset? expiresAt = null);

    Task ConfirmReservationAsync(Guid reservationId);

    Task ReleaseReservationAsync(Guid reservationId);

    Task CancelReservationAsync(Guid reservationId);

    Task ExpireReservationsAsync();

    Task<IReadOnlyCollection<StockReservation>> GetActiveReservationsAsync(Guid stockId);
}
