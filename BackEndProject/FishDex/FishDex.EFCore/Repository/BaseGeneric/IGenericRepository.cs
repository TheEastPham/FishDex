using System.Linq.Expressions;

namespace FishDex.EFCore.Repository.BaseGeneric;

public interface IGenericRepository<T> where T : class
{
    // Get operations
    Task<T?> GetByIdAsync<TKey>(TKey id);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    
    // Add operations
    Task<T> AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
}