using AEHotel.Domain.Common;

namespace AEHotel.Repository.Repositories.Interfaces;

public interface IBaseRepository<T> where T : BaseEntity
{
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    void Remove(T entity);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
