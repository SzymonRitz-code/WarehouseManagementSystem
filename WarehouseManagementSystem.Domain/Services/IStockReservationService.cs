namespace WarehouseManagementSystem.Domain.Services;

/// <summary>
/// Defines periodic operations for handling stock reservations.
/// </summary>
public interface IStockReservationService
{
    /// <summary>
    /// Expires active reservations whose expiration time has passed.
    /// </summary>
    /// <param name="ct">Operation cancellation token.</param>
    /// <returns>A task representing the reservation expiration operation.</returns>
    Task ExpireReservationsAsync(CancellationToken ct = default);
}
