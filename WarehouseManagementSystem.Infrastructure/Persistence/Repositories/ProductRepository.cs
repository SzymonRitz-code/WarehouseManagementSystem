using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WarehouseManagementSystem.Domain.Interfaces.Repositories;
using WarehouseManagementSystem.Domain.Model.CatalogDomain;

namespace WarehouseManagementSystem.Infrastructure.Persistence.Repositories
{
    internal class ProductRepository : IProductRepository
    {
        private readonly WarehouseManagementSystemDbContext _context;

        public ProductRepository(WarehouseManagementSystemDbContext context)
        {
            _context = context;
        }

        public void Add(Product entity)
        {
            _context.Products.Add(entity);
        }

        public async Task<IEnumerable<Product>> AllAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public bool Any(Expression<Func<Product, bool>> predicate)
        {
            return _context.Products.Any(predicate);
        }

        public void Delete(Product entity)
        {
            _context.Products.Remove(entity);
        }

        public Product Find(Guid id)
        {
            return _context.Products.Find(id);
        }

        public async Task<Product> FindAsync(Guid id)
        {
            return await _context.Products.FindAsync(id);
        }

        public Product Update(Product entity)
        {
            return _context.Products.Update(entity).Entity;
        }

        public void UpdateRange(IEnumerable<Product> entities)
        {
            _context.Products.UpdateRange(entities);
        }
    }
}