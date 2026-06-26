using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Interfaces.Repositories;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

namespace WarehouseManagementSystem.Infrastructure.Persistence.Repositories
{
    internal class WarehouseRepository : IWarehouseRepository
    {
        private readonly WarehouseManagementSystemDbContext _context;

        public WarehouseRepository(WarehouseManagementSystemDbContext context)
        {
            this._context = context;
        }

        public void Add(Warehouse entity)
        {
            _context.Warehouses.Add(entity);
        }

        public async Task<IEnumerable<Warehouse>> AllAsync()
        {
            return await _context.Warehouses.AsNoTracking().ToListAsync();
        }

        public bool Any(Expression<Func<Warehouse, bool>> predicate)
        {
            return _context.Warehouses.AsNoTracking().Any(predicate);
        }

        public void Delete(Warehouse entity)
        {
            _context.Warehouses.Remove(entity);
        }

        public Warehouse Find(Guid id)
        {
            return _context.Warehouses.Find(id);
        }

        public async Task<Warehouse> FindAsync(Guid id)
        {
            return await _context.Warehouses.FindAsync(id);
        }

        public Warehouse Update(Warehouse entity)
        {
            _context.Warehouses.Update(entity);
            return entity;
        }

        public void UpdateRange(IEnumerable<Warehouse> entities)
        {
            _context.Warehouses.UpdateRange(entities);
        }
    }
}
