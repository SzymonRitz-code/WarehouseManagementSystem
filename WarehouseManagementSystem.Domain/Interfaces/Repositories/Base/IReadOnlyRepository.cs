using System.Linq.Expressions;

namespace WarehouseManagementSystem.Domain.Interfaces.Repositories.Base;

public interface IReadOnlyRepository<TEntity> where TEntity : class
{
    TEntity Find(Guid id);
    Task<TEntity> FindAsync(Guid id);
    bool Any(Expression<Func<TEntity, bool>> predicate);
}