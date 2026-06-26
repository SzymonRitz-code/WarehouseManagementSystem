using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Interfaces.Repositories;
using WarehouseManagementSystem.Domain.Model.InventoryDomain;

namespace WarehouseManagementSystem.Infrastructure.Persistence.Repositories
{
    public class ProductBatchRepository : IProductBatchRepository
    {
        private readonly WarehouseManagementSystemDbContext _context;

        public ProductBatchRepository(WarehouseManagementSystemDbContext context)
        {
            _context = context;
        }

        public void Add(ProductBatch entity)
        {
            _context.Add(entity);
        }

        public IEnumerable<ProductBatch> All()
        {
            return _context.ProductBatches.AsNoTracking();
        }

        public async Task<IEnumerable<ProductBatch>> AllAsync()
        {
            return await _context.ProductBatches.AsNoTracking().ToListAsync();
        }

        public bool Any(Expression<Func<ProductBatch, bool>> predicate)
        {
            return _context.ProductBatches.AsNoTracking().Any(predicate);
        }

        public void Delete(ProductBatch entity)
        {
            _context.ProductBatches.Remove(entity);
        }

        public ProductBatch Find(Guid id)
        {
            return _context.ProductBatches.Find(id);
        }

        public async Task<ProductBatch> FindAsync(Guid id)
        {
            return await _context.ProductBatches.FindAsync(id);
        }

        public ProductBatch Update(ProductBatch entity)
        {
            return _context.ProductBatches.Update(entity).Entity;
        }

        public void UpdateRange(IEnumerable<ProductBatch> entities)
        {
            _context.UpdateRange(entities);
        }
    }
}
