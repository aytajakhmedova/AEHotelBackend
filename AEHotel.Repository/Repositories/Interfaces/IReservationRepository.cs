using AEHotel.Domain.Entities;

namespace AEHotel.Repository.Repositories.Interfaces;

public interface IReservationRepository
{
    Task<IReadOnlyList<Reservation>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Reservation?> GetByIdAsync(int id, bool tracking, bool includeDetails, CancellationToken cancellationToken = default);

    Task<bool> HasOverlapAsync(
        int roomId,
        DateOnly checkIn,
        DateOnly checkOut,
        int? excludeReservationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default);

    void Remove(Reservation reservation);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
