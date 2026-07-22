using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WarehouseManagementSystem.Domain.Interfaces;
using WarehouseManagementSystem.Domain.Interfaces.Repositories;
using WarehouseManagementSystem.Infrastructure.Persistence.Repositories;

namespace WarehouseManagementSystem.Infrastructure.Persistence;

/// <summary>
/// UNIT OF WORK PATTERN - Transaction Management in WMS Architecture
/// 
/// PURPOSE:
/// The Unit of Work pattern provides an explicit mechanism for managing database transactions.
/// It coordinates multiple repository operations and ensures they all succeed or all fail together (ACID).
/// 
/// ARCHITECTURAL CONTEXT:
/// This is a cornerstone of the WMS Event-Driven CQRS architecture.
/// It represents the "transaction boundary" - the scope of consistency.
/// 
/// KEY DESIGN DECISIONS:
/// 
/// 1. EXPLICIT TRANSACTION CONTROL
///    ┌─────────────────────────────────────────────────────────┐
///    │ CommandService Usage:                                   │
///    │                                                          │
///    │ using (var uow = new UnitOfWork(context))              │
///    │ {                                                       │
///    │     var document = await uow.Documents.GetAsync(id);  │
///    │     document.Confirm(user);                           │
///    │     await uow.SaveChangesAsync();  ← EXPLICIT call    │
///    │ }                                                       │
///    │                                                          │
///    │ ADVANTAGE: Clear where transactions commit              │
///    │ DISADVANTAGE: Easy to forget SaveChangesAsync() call   │
///    └─────────────────────────────────────────────────────────┘
/// 
/// 2. CENTRALIZED REPOSITORY ACCESS
///    All repositories accessed through UnitOfWork properties (Documents, Products, etc)
///    Ensures repositories share same DbContext (= same transaction)
///    
///    Without UnitOfWork:
///    ❌ var doc = await documentRepo.GetAsync(id);     ← DbContext #1
///    ❌ var product = await productRepo.GetAsync(id);  ← DbContext #2 - DIFFERENT!
///    ❌ await uow.SaveChangesAsync();                   ← Saves to both, inconsistent
///    
///    With UnitOfWork:
///    ✅ var doc = await uow.Documents.GetAsync(id);    ← DbContext shared
///    ✅ var product = await uow.Products.GetAsync(id); ← Same DbContext
///    ✅ await uow.SaveChangesAsync();                   ← Atomic save
/// 
/// 3. DISTRIBUTED TRANSACTION SUPPORT
///    UnitOfWork can manage explicit transactions:
///    
///    using (var uow = new UnitOfWork(context))
///    using (var txn = await uow.BeginTransactionAsync(IsolationLevel.Serializable))
///    {
///        try
///        {
///            var doc = await uow.Documents.GetAsync(id);
///            doc.Confirm(user);
///            await uow.SaveChangesAsync();
///            await txn.CommitAsync();
///        }
///        catch
///        {
///            // Automatic rollback on dispose
///        }
///    }
///    
///    COMPARISON WITH DDD-Fundamentals:
///    - DDD-Fundamentals: Implicit in DbContext (no explicit UnitOfWork)
///    - WMS: Explicit UnitOfWork for complex scenarios
/// 
/// FLOW COMPARISON:
/// 
/// DDD-Fundamentals (Implicit):
/// ┌────────────────┐
/// │  Endpoint      │
/// │  handler       │
/// └────────┬───────┘
///          │
///    ┌─────v──────────────┐
///    │ repo.GetAsync()    │
///    │ aggregate.DoWork() │
///    │ repo.AddAsync()    │
///    └─────┬──────────────┘
///          │
///    ┌─────v────────────────────────────┐
///    │ context.SaveChangesAsync()       │
///    │ ↓                                │
///    │ Auto-publish events via MediatR  │
///    └────────────────────────────────┘
/// 
/// WMS (Explicit):
/// ┌────────────────────────┐
/// │  CommandService        │
/// │  + UnitOfWork          │
/// └────────┬───────────────┘
///          │
///    ┌─────v──────────────────┐
///    │ uow.Documents.GetAsync()
///    │ aggregate.Confirm()    │
///    │ uow.SaveChangesAsync() ← EXPLICIT
///    └─────┬──────────────────┘
///          │
///    ┌─────v──────────────────────────┐
///    │ EventPublisher.PublishAsync()   │
///    │ (manual in CommandService)      │
///    └────────────────────────────────┘
/// 
/// CONSISTENCY GUARANTEES:
/// 
/// 1. ACID Properties:
///    - Atomicity: All repos save together or not at all
///    - Consistency: Aggregates enforce invariants before SaveChanges
///    - Isolation: Managed by DbContext + IsolationLevel (BeginTransactionAsync)
///    - Durability: SQL Server ensures committed data persists
/// 
/// 2. Event Publishing Consistency:
///    In WMS, events are published AFTER SaveChangesAsync():
///    
///    await uow.SaveChangesAsync();        // ← Data persisted to DB
///    await eventPublisher.Publish(evt);  // ← Event handlers execute
///    
///    RISK: If process crashes between these two lines, event might be lost
///    MITIGATION: Use transactional outbox pattern (not shown here)
///    
///    DDD-Fundamentals handles this via SaveChangesAsync() + MediatR integration:
///    public override async Task<int> SaveChangesAsync(...)
///    {
///        int result = await base.SaveChangesAsync(...);  // Persist data
///        // Auto-publish events from BaseEntity.Events    // Guaranteed
///        foreach (var evt in entitiesWithEvents)
///            await _mediator.Publish(evt);
///    }
/// 
/// REPOSITORY INITIALIZATION PATTERN:
/// Repositories are created in constructor, not lazy-loaded.
/// 
/// ADVANTAGES:
/// ✅ Fail-fast: If repository creation fails, caught immediately
/// ✅ No null checks needed (repositories always initialized)
/// ✅ All repos share same DbContext
/// 
/// ALTERNATIVE (Lazy Loading):
/// ❌ private IAuditLogRepository _auditLogRepository;
/// ❌ public IAuditLogRepository AuditLogs 
/// ❌ {
/// ❌     get { return _auditLogRepository ??= new AuditLogRepository(_context); }
/// ❌ }
/// ❌ (Creates repo on first access - can cause unexpected delays)
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    /// <summary>
    /// DbContext wraps the database connection and change tracking.
    /// All repositories share this context to ensure transactional consistency.
    /// </summary>
    private readonly WarehouseManagementSystemDbContext _context;

    /// <summary>
    /// REPOSITORY INSTANCES - One per aggregate root.
    /// These are instantiated once in constructor (not lazy).
    /// All share the same _context = all part of same transaction.
    /// </summary>
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductBatchRepository _productBatchRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IWarehouseZoneRepository _warehouseZoneRepository;

    /// <summary>
    /// CONSTRUCTOR: Initialize UnitOfWork with DbContext.
    /// 
    /// FLOW:
    /// 1. Validate context (throw if null)
    /// 2. Create all repository instances (sharing _context)
    /// 3. Now ready to use
    /// 
    /// EXAMPLE USAGE (in CommandService):
    /// var uow = new UnitOfWork(dbContext);
    /// var doc = await uow.Documents.GetAsync(id);
    /// doc.Confirm(user);
    /// await uow.SaveChangesAsync(); // All changes atomic
    /// </summary>
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

    /// <summary>
    /// REPOSITORY PROPERTIES - Access repositories through this interface.
    /// 
    /// DESIGN RATIONALE:
    /// - All exposed via properties (not methods)
    /// - Each property returns private field (no recreation each time)
    /// - CommandService depends on IUnitOfWork interface, not this concrete class
    /// 
    /// USAGE:
    /// var document = await uow.Documents.GetAsync(documentId);
    /// var products = await uow.Products.ListAsync();
    /// await uow.Products.AddAsync(newProduct);
    /// </summary>
    public IAuditLogRepository AuditLogs => _auditLogRepository;
    public IProductRepository Products => _productRepository;
    public IProductBatchRepository ProductBatches => _productBatchRepository;
    public IStockRepository Stocks => _stockRepository;
    public IDocumentRepository Documents => _documentRepository;
    public IWarehouseRepository Warehouses => _warehouseRepository;
    public IWarehouseZoneRepository WarehouseZones => _warehouseZoneRepository;

    /// <summary>
    /// CLEANUP: Dispose DbContext and resources.
    /// 
    /// Called when:
    /// - Using statement exits: using (var uow = new UnitOfWork(...)) { ... }
    /// - Manually: uow.Dispose()
    /// 
    /// IMPORTANT: Failing to dispose causes connection leak and memory leak.
    /// Always use 'using' statement:
    /// 
    /// ✅ using (var uow = new UnitOfWork(context))
    ///    {
    ///        // Use UnitOfWork
    ///    } // Automatically disposed
    /// 
    /// ❌ var uow = new UnitOfWork(context);
    ///    // ... operations ...
    ///    // uow.Dispose() NEVER CALLED - LEAK!
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
    }

    /// <summary>
    /// SYNCHRONOUS SAVE - Blocking call.
    /// 
    /// WHEN TO USE:
    /// - Legacy code that must be synchronous
    /// - Very rare in modern async/await code
    /// 
    /// IMPLEMENTATION:
    /// Calls async SaveChangesAsync() and blocks waiting for result.
    /// Blocks thread pool thread (avoid if possible).
    /// 
    /// BETTER: Use SaveChangesAsync() instead (non-blocking).
    /// </summary>
    public int SaveChanges()
    {
        return _context.SaveChanges();
    }

    /// <summary>
    /// ASYNCHRONOUS SAVE - Primary method for persistence.
    /// 
    /// FLOW:
    /// 1. Entity Framework tracks all changes to entities managed by _context
    /// 2. SaveChangesAsync() sends SQL INSERT/UPDATE/DELETE to database
    /// 3. Database processes commands inside a transaction (auto by EF)
    /// 4. Returns count of affected rows
    /// 5. DbContext clears change tracker (entities now "clean")
    /// 
    /// TRANSACTION BOUNDARIES:
    /// By default, SaveChangesAsync() wraps all changes in single transaction:
    /// 
    /// BEGIN TRANSACTION
    ///   INSERT INTO Documents ...
    ///   UPDATE Products ...
    ///   DELETE FROM Stock ...
    /// COMMIT TRANSACTION
    /// 
    /// If ANY command fails, entire transaction rolled back (ACID).
    /// 
    /// WHEN TO CALL:
    /// After all domain operations complete (aggregate state changes).
    /// 
    /// TYPICAL FLOW IN COMMANDSERVICE:
    /// var document = await uow.Documents.GetAsync(id);        // Load
    /// document.Confirm(user);                                  // Modify state
    /// int rowsAffected = await uow.SaveChangesAsync(ct);      // Persist
    /// if (rowsAffected > 0)
    ///     await eventPublisher.PublishAsync(evt);  // Side effects
    /// 
    /// COMPARISON WITH DDD-Fundamentals:
    /// - DDD: SaveChangesAsync() auto-publishes events (MediatR integration)
    /// - WMS: SaveChangesAsync() just persists, event publishing is separate
    /// </summary>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// EXPLICIT TRANSACTION MANAGEMENT - For complex multi-step operations.
    /// 
    /// PURPOSE:
    /// Sometimes SaveChangesAsync() alone isn't enough. You need:
    /// - Custom isolation level (Serializable, RepeatableRead, etc)
    /// - Multiple SaveChanges() within single transaction
    /// - Nested transaction savepoints
    /// - Distributed transaction coordination
    /// 
    /// USAGE EXAMPLE (Complex Warehouse Transfer):
    /// using (var uow = new UnitOfWork(context))
    /// using (var txn = await uow.BeginTransactionAsync(IsolationLevel.Serializable))
    /// {
    ///     try
    ///     {
    ///         // Step 1: Remove from source warehouse
    ///         var sourceStock = await uow.Stocks.GetAsync(stockId);
    ///         sourceStock.Reduce(quantity);
    ///         await uow.SaveChangesAsync(ct);  // Save step 1
    ///         
    ///         // Step 2: Add to target warehouse (in same transaction)
    ///         var targetStock = await uow.Stocks.GetOrCreateAsync(targetStockId);
    ///         targetStock.Increase(quantity);
    ///         await uow.SaveChangesAsync(ct);  // Save step 2
    ///         
    ///         // Both succeeded, commit
    ///         await txn.CommitAsync(ct);
    ///     }
    ///     catch (Exception)
    ///     {
    ///         // Either step failed, entire transaction rolled back
    ///         // No partial data!
    ///         throw;
    ///     }
    /// } // txn disposed = rollback if not committed
    /// 
    /// ISOLATION LEVELS:
    /// - ReadUncommitted: Dirty reads allowed (rare, dangerous)
    /// - ReadCommitted: Default, only reads committed data
    /// - RepeatableRead: No "phantom" rows (lock rows read)
    /// - Serializable: Strictest, acts like single-threaded (slowest)
    /// 
    /// COMPARISON WITH DDD-Fundamentals:
    /// - DDD: Implicit transactions, difficult to customize isolation level
    /// - WMS: Explicit transactions, full control
    /// </summary>
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        return new EfUnitOfWorkTransaction(transaction);
    }

    /// <summary>
    /// ACTIVE TRANSACTION CHECK - Is there an uncommitted transaction?
    /// 
    /// Useful for:
    /// - Debugging (ensure you're in transaction when expected)
    /// - Conditional logic (some operations only valid inside transaction)
    /// 
    /// RETURNS:
    /// true = Active transaction exists (must call Commit or Dispose to close)
    /// false = No transaction (auto-commit mode)
    /// </summary>
    public bool HasActiveTransaction => _context.Database.CurrentTransaction != null;

    /// <summary>
    /// INTERNAL ADAPTER: Wraps EF's IDbContextTransaction as IUnitOfWorkTransaction.
    /// 
    /// DESIGN PATTERN: Adapter/Bridge
    /// - Hides EF dependency from command services
    /// - IUnitOfWorkTransaction is domain-aware interface
    /// - Allows swapping EF for different ORM later (if needed)
    /// 
    /// METHODS:
    /// - CommitAsync(): Persist all changes from this transaction
    /// - DisposeAsync(): Cleanup (auto-rollback if not committed)
    /// </summary>
    private sealed class EfUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public EfUnitOfWorkTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        /// <summary>
        /// COMMIT: Persist all changes and complete transaction.
        /// 
        /// After CommitAsync():
        /// - All changes from SaveChangesAsync() are durable in database
        /// - Other connections can see the changes
        /// - Transaction is closed
        /// 
        /// If called again after disposed:
        /// - Throws ObjectDisposedException
        /// </summary>
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return _transaction.CommitAsync(cancellationToken);
        }

        /// <summary>
        /// ASYNC CLEANUP: Close transaction and release resources.
        /// 
        /// AUTOMATIC BEHAVIOR:
        /// If transaction still active (not committed) when disposed:
        /// - Automatic ROLLBACK happens
        /// - All changes in this transaction are discarded
        /// 
        /// CORRECT USAGE:
        /// using (var txn = await uow.BeginTransactionAsync())
        /// {
        ///     // ... operations ...
        ///     await txn.CommitAsync();  // Success: explicit commit
        /// } // Dispose: automatic rollback if exception occurred
        /// </summary>
        public ValueTask DisposeAsync()
        {
            return _transaction.DisposeAsync();
        }
    }
}

