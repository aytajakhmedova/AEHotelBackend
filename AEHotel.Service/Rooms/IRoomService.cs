using AEHotel.App.Rooms;

namespace AEHotel.Service.Rooms;

public interface IRoomService
{
    Task<IReadOnlyList<RoomResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoomResponse>> GetByHotelIdAsync(int hotelId, CancellationToken cancellationToken = default);

    Task<RoomResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<RoomResponse> CreateAsync(RoomRequest request, CancellationToken cancellationToken = default);

    Task<RoomResponse> UpdateAsync(int id, RoomRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
