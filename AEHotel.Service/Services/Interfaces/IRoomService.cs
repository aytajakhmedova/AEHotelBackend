using AEHotel.Service.DTOs.Rooms;

namespace AEHotel.Service.Services.Interfaces;

public interface IRoomService
{
    Task<IReadOnlyList<RoomResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoomResponse>> GetByHotelIdAsync(int hotelId, CancellationToken cancellationToken = default);

    Task<RoomResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<RoomResponse> CreateAsync(RoomRequest request, CancellationToken cancellationToken = default);

    Task<RoomResponse> UpdateAsync(int id, RoomRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<RoomQuoteResponse> QuoteAsync(int id, DateOnly checkIn, DateOnly checkOut, int guests, int? excludeBookingId = null, CancellationToken cancellationToken = default);
}
