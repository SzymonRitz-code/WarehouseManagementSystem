using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WarehouseManagementSystem.Domain.Interfaces.Repositories;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.Infrastructure.Persistence.Repositories
{
    internal class StockRepository : IStockRepository
    {
        private readonly WarehouseManagementSystemDbContext _context;

        public StockRepository(WarehouseManagementSystemDbContext context)
        {
            _context = context;
        }

        public void Add(Stock entity)
        {
            _context.Stocks.Add(entity);
        }

        public bool Any(Expression<Func<Stock, bool>> predicate)
        {
            return _context.Stocks.Any(predicate);
        }

        public void Delete(Stock entity)
        {
            _context.Remove(entity);
        }

        public Stock Find(Guid id)
        {
            return _context.Stocks.Find(id);
        }

        public async Task<Stock> FindAsync(Guid id)
        {
            return await _context.Stocks.FindAsync(id);
        }

        public async Task<Stock?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, Guid warehouseZoneId, Guid? batchId)
        {
            return await _context.Stocks
                .FirstOrDefaultAsync(s =>
                    s.ProductId == productId &&
                    s.WarehouseId == warehouseId &&
                    s.WarehouseZoneId == warehouseZoneId &&
                    s.ProductBatchId == batchId);
        }

        public Stock Update(Stock entity)
        {
            return _context.Stocks.Update(entity).Entity;
        }

        public void UpdateRange(IEnumerable<Stock> entities)
        {
            _context.Stocks.UpdateRange(entities);
        }
    }
}
