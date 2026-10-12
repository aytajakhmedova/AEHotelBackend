using AEHotel.Domain.Entities;

namespace AEHotel.Repository.Rooms;

public interface IRoomRepository
{
    Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Room>> GetByHotelIdAsync(int hotelId, CancellationToken cancellationToken = default);

    Task<Room?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken = default);

    Task<bool> HasReservationsAsync(int roomId, CancellationToken cancellationToken = default);

    Task AddAsync(Room room, CancellationToken cancellationToken = default);

    void Remove(Room room);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
