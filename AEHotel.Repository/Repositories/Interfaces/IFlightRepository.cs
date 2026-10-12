using AEHotel.Domain.Entities;

namespace AEHotel.Repository.Repositories.Interfaces;

public interface IFlightRepository
{
    Task<IReadOnlyList<Flight>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Flight?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken = default);

    Task AddAsync(Flight flight, CancellationToken cancellationToken = default);

    void Remove(Flight flight);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
