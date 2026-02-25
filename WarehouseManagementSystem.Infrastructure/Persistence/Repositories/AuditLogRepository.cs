using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WarehouseManagementSystem.Domain.Interfaces.Repositories;
using WarehouseManagementSystem.Domain.Model.AuditDomain;

namespace WarehouseManagementSystem.Infrastructure.Persistence.Repositories
{
    internal class AuditLogRepository : IAuditLogRepository
    {
        private readonly WarehouseManagementSystemDbContext _context;

        public AuditLogRepository(WarehouseManagementSystemDbContext context)
        {
            _context = context;
        }

        public void Add(AuditLog entity)
        {
            throw new NotImplementedException();
        }

        public bool Any(Expression<Func<AuditLog, bool>> predicate)
        {
            throw new NotImplementedException();
        }

        public void Delete(AuditLog entity)
        {
            throw new NotImplementedException();
        }

        public AuditLog Find(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<AuditLog> FindAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<AuditLog>> GetFilteredAsync(
            string? entityName,
            Guid? entityId,
            Guid? performedById)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(entityName))
                query = query.Where(x => x.EntityName == entityName);

            if (entityId.HasValue)
                query = query.Where(x => x.EntityId == entityId.Value);

            if (performedById.HasValue)
                query = query.Where(x => x.PerformedById == performedById.Value);

            return await query.AsNoTracking()
                .OrderByDescending(x => x.PerformedAt)
                .ToListAsync();
        }

        public AuditLog Update(AuditLog entity)
        {
            throw new NotImplementedException();
        }

        public void UpdateRange(IEnumerable<AuditLog> entities)
        {
            throw new NotImplementedException();
        }
    }
}