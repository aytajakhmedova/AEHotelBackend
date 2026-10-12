using AEHotel.Domain.Entities;

namespace AEHotel.Repository.Repositories.Interfaces;

public interface IHotelRepository
{
    Task<IReadOnlyList<Hotel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Hotel?> GetByIdAsync(int id, bool includeRooms, bool tracking, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    Task<string?> GetTitleAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> HasReservationsAsync(int hotelId, CancellationToken cancellationToken = default);

    Task AddAsync(Hotel hotel, CancellationToken cancellationToken = default);

    void Remove(Hotel hotel);

    Task LoadRoomsAsync(Hotel hotel, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
