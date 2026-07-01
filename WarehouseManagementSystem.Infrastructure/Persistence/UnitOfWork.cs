using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Interfaces.Repositories;
using WarehouseManagementSystem.Infrastructure.Persistence.Repositories;

namespace WarehouseManagementSystem.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly WarehouseManagementSystemDbContext _context;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductBatchRepository _productBatchRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IWarehouseZoneRepository _warehouseZoneRepository;

    public UnitOfWork(WarehouseManagementSystemDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogRepository = new AuditLogRepository(_context);
        _productRepository = new ProductRepository(_context);
        _productBatchRepository = new ProductBatchRepository(_context);
        _stockRepository = new StockRepository(_context);
        _documentRepository = new DocumentRepository(_context);
        _warehouseRepository = new WarehouseRepository(_context);
        _warehouseZoneRepository = new WarehouseZoneRepository(_context);
    }

    public IAuditLogRepository AuditLogs => _auditLogRepository;
    public IProductRepository Products => _productRepository;
    public IProductBatchRepository ProductBatches => _productBatchRepository;
    public IStockRepository Stocks => _stockRepository;
    public IDocumentRepository Documents => _documentRepository;
    public IWarehouseRepository Warehouses => _warehouseRepository;
    public IWarehouseZoneRepository WarehouseZones => _warehouseZoneRepository;

    public void Dispose()
    {
        _context.Dispose();
    }

    public int SaveChanges()
    {
        return _context.SaveChanges();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        return new EfUnitOfWorkTransaction(transaction);
    }

    public bool HasActiveTransaction => _context.Database.CurrentTransaction != null;

    private sealed class EfUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public EfUnitOfWorkTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return _transaction.CommitAsync(cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return _transaction.DisposeAsync();
        }
    }
}
