using WarehouseManagementSystem.Domain.Interfaces.Repositories;

namespace WarehouseManagementSystem.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IAuditLogRepository AuditLogs { get; }
    IProductBatchRepository ProductBatches { get; }
    IStockRepository Stocks { get; }
    IStockReservationRepository StockReservations { get; }
    IDocumentRepository Documents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}
