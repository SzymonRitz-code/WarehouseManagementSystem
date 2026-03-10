namespace WarehouseManagementSystem.Domain.Interfaces.Repositories.Base;

public interface IRepository<TEntity> : IReadOnlyRepository<TEntity> where TEntity : class
{
    void Add(TEntity entity);
    TEntity Update(TEntity entity);
    void UpdateRange(IEnumerable<TEntity> entities);
    void Delete(TEntity entity);
}
