using AEHotel.Domain.Common;
using AEHotel.Repository.Data;
using AEHotel.Repository.Repositories.Interfaces;

namespace AEHotel.Repository.Repositories;

public class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Context;

    protected BaseRepository(AppDbContext context)
    {
        Context = context;
    }

    public Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        return Context.Set<T>().AddAsync(entity, cancellationToken).AsTask();
    }

    public void Remove(T entity)
    {
        Context.Set<T>().Remove(entity);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Context.SaveChangesAsync(cancellationToken);
    }
}
