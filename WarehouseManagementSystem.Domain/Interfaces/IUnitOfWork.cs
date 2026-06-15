using System.Data;
using WarehouseManagementSystem.Domain.Interfaces.Repositories;

namespace WarehouseManagementSystem.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IAuditLogRepository AuditLogs { get; }
    IProductRepository Products { get; }
    IProductBatchRepository ProductBatches { get; }
    IStockRepository Stocks { get; }
    IDocumentRepository Documents { get; }
    IWarehouseRepository Warehouses { get; }
    IWarehouseZoneRepository WarehouseZones { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);
}
