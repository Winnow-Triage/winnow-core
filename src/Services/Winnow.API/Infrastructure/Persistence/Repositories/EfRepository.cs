using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Core;

namespace Winnow.API.Infrastructure.Persistence.Repositories;

public class EfRepository<T>(WinnowDbContext dbContext) : IRepository<T> where T : class, IAggregateRoot
{
    private readonly WinnowDbContext _dbContext = dbContext;

    protected WinnowDbContext DbContext => _dbContext;

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>().FindAsync([id], cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>().ToListAsync(cancellationToken);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbContext.Set<T>().AddAsync(entity, cancellationToken);
    }

    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbContext.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbContext.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.SaveChangesAsync(cancellationToken);
    }
}
