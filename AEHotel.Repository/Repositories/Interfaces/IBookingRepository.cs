using AEHotel.Domain.Entities;

namespace AEHotel.Repository.Repositories.Interfaces;

public interface IBookingRepository
{
    Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Booking?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken = default);

    Task<bool> HasHotelOverlapAsync(int roomId, DateOnly checkIn, DateOnly checkOut, int? excludeBookingId, CancellationToken cancellationToken = default);

    Task<List<int>> GetBusyRoomIdsAsync(DateOnly checkIn, DateOnly checkOut, CancellationToken cancellationToken = default);

    Task<int> CountTourGuestsAsync(int tourId, DateOnly travelDate, int? excludeBookingId, CancellationToken cancellationToken = default);

    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
