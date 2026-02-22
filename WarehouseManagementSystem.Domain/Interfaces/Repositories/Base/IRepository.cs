using System.Linq.Expressions;

namespace WarehouseManagementSystem.Domain.Interfaces.Repositories.Base;

public interface IRepository<TEntity> where TEntity : class
{
    void Add(TEntity entity);
    TEntity Update(TEntity entity);
    void UpdateRange(IEnumerable<TEntity> entities);
    TEntity Find(Guid id);
    Task<TEntity> FindAsync(Guid id);
    bool Any(Expression<Func<TEntity, bool>> predicate);
    void Delete(TEntity entity);


}
