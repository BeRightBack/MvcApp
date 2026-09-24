using System.Linq;
using System.Linq.Expressions;

namespace MvcApp.Core.Abstractions;

public interface IRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> Query();
    IEnumerable<TEntity> GetAll();
    Task<IEnumerable<TEntity>> GetAllAsync();
    TEntity Get(Guid id);
    Task<TEntity> GetAsync(Guid id);
    void Add(TEntity entity);
    Task AddAsync(TEntity entity);
    void Update(TEntity entity);
    Task UpdateAsync(TEntity entity);
    void Delete(TEntity entity);
    Task DeleteAsync(TEntity entity);
    Task<TEntity?> GetByIdAsync(int? id);
    Task<TEntity?> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);
    Task DeleteAsync(int id);
    Task DeleteUserAsync(string id);
}
