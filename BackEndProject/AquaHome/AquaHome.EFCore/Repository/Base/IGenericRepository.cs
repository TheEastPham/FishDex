using System.Linq.Expressions;

namespace AquaHome.EFCore.Repository.Base;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync<TKey>(TKey id);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}
