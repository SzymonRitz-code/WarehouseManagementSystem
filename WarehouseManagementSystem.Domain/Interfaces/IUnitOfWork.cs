using WarehouseManagementSystem.Domain.Interfaces.Repositories;

namespace WarehouseManagementSystem.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IStockRepository Stocks { get; }
    IStockReservationRepository StockReservations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}
