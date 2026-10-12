using AEHotel.Domain.Entities;

namespace AEHotel.Repository.Repositories.Interfaces;

public interface ITourRepository
{
    Task<IReadOnlyList<Tour>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Tour?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken = default);

    Task AddAsync(Tour tour, CancellationToken cancellationToken = default);

    void Remove(Tour tour);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
