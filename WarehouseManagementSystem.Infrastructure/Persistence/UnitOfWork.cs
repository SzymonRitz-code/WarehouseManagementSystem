using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Interfaces.Repositories;
using WarehouseManagementSystem.Infrastructure.Persistence.Repositories;

namespace WarehouseManagementSystem.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IProductBatchRepository _productBatches;
    private readonly IStockRepository _stockRepository;
    private readonly IStockReservationRepository _stockReservationRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly WarehouseManagementSystemDbContext _context;

    public UnitOfWork(WarehouseManagementSystemDbContext context)
    {
        _context = context ?? throw new NullReferenceException();
    }
    public IAuditLogRepository AuditLogs { get { return _auditLogRepository ?? new AuditLogRepository(_context); } }
    public IProductBatchRepository ProductBatches { get { return _productBatches ?? new ProductBatchRepository(_context); } }
    public IStockRepository Stocks { get { return _stockRepository ?? new StockRepository(_context); } }

    public IStockReservationRepository StockReservations { get { return _stockReservationRepository ?? new StockReservationRepository(_context); } }

    public IDocumentRepository Documents { get { return _documentRepository ?? new DocumentRepository(_context); } }



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
}

