using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Domain.Interfaces.Repositories;
using WarehouseManagementSystem.Domain.Model.AuditDomain;

namespace WarehouseManagementSystem.Infrastructure.Persistence.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly WarehouseManagementSystemDbContext _context;

        public AuditLogRepository(WarehouseManagementSystemDbContext context)
        {
            _context = context;
        }

        public void Add(AuditLog entity)
        {
            _context.Add(entity);
        }

        public bool Any(Expression<Func<AuditLog, bool>> predicate)
        {
            return _context.AuditLogs.Any(predicate);
        }
        public AuditLog Find(Guid id)
        {
            return _context.AuditLogs.Find(id);
        }

        public async Task<AuditLog> FindAsync(Guid id)
        {
            return await _context.AuditLogs.FindAsync(id);
        }

        public async Task<IEnumerable<AuditLog>> GetFilteredAsync(
            string? entityName,
            Guid? entityId,
            Guid? performedById)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(entityName))
            {
                query = query.Where(x => x.EntityName == entityName);
            }

            if (entityId.HasValue)
            {
                query = query.Where(x => x.EntityId == entityId.Value);
            }

            if (performedById.HasValue)
            {
                query = query.Where(x => x.PerformedById == performedById.Value);
            }

            return await query.AsNoTracking()
                .OrderByDescending(x => x.PerformedAt)
                .ToListAsync();
        }
    }
}