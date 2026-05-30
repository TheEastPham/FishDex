using System.Linq.Expressions;
using AquaHome.EFCore.Data;
using Microsoft.EntityFrameworkCore;

namespace AquaHome.EFCore.Repository.Base;

public class GenericRepository<T>(AquaHomeDbContext context) : IGenericRepository<T>
    where T : class
{
    private readonly DbSet<T> _set = context.Set<T>();

    public virtual Task<T?> GetByIdAsync<TKey>(TKey id) => _set.FindAsync(id).AsTask();

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => await _set.Where(predicate).ToListAsync();

    public virtual async Task<T> AddAsync(T entity)
    {
        await _set.AddAsync(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        _set.Update(entity);
        await context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(T entity)
    {
        _set.Remove(entity);
        await context.SaveChangesAsync();
    }
}
