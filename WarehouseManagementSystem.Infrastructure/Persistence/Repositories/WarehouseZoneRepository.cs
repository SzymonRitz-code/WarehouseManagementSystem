using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Interfaces.Repositories;
using WarehouseManagementSystem.Domain.Model.WarehouseDomain;

namespace WarehouseManagementSystem.Infrastructure.Persistence.Repositories
{
    public class WarehouseZoneRepository : IWarehouseZoneRepository
    {
        private readonly WarehouseManagementSystemDbContext _context;

        public WarehouseZoneRepository(WarehouseManagementSystemDbContext context)
        {
            _context = context;
        }

        public void Add(WarehouseZone entity)
        {
            _context.Add(entity);
        }

        public async Task<IEnumerable<WarehouseZone>> AllAsync()
        {
            return await _context.WarehouseZones.AsNoTracking().ToListAsync();
        }

        public bool Any(Expression<Func<WarehouseZone, bool>> predicate)
        {
            return _context.WarehouseZones.AsNoTracking().Any(predicate);
        }

        public void Delete(WarehouseZone entity)
        {
            _context.WarehouseZones.Remove(entity);
        }

        public WarehouseZone Find(Guid id)
        {
            return _context.WarehouseZones.Find(id);
        }

        public async Task<WarehouseZone> FindAsync(Guid id)
        {
            return await _context.WarehouseZones.FindAsync(id);
        }

        public WarehouseZone Update(WarehouseZone entity)
        {
            return _context.Update(entity).Entity;
        }

        public void UpdateRange(IEnumerable<WarehouseZone> entities)
        {
            _context.UpdateRange(entities);
        }
    }
}
