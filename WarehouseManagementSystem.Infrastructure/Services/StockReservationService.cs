using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Services;

namespace WarehouseManagementSystem.Infrastructure.Services;

public class StockReservationService : IStockReservationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemClock _clock;

    public StockReservationService(IUnitOfWork unitOfWork, ISystemClock clock)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task ExpireReservationsAsync()
    {
        var now = _clock.UtcNow;

        // Pobierz wszystkie aktywne rezerwacje, które już wygasły
        var expiredReservations = await _unitOfWork.Stocks.GetExpiredReservationsAsync(now);

        foreach (var reservation in expiredReservations)
        {
            var stock = await _unitOfWork.Stocks.FindAsync(reservation.StockId);
            if (stock == null) continue;

            // Wywołanie agregatu domenowego Stock do wygaszenia rezerwacji
            stock.ExpireReservation(reservation.Id);
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
