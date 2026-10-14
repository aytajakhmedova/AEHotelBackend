using AEHotel.Service.DTOs.Bookings;

namespace AEHotel.Service.Services.Interfaces;

public interface IBookingService
{
    Task<IReadOnlyList<BookingResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingResponse>> GetMineAsync(string userId, CancellationToken cancellationToken = default);

    Task<BookingResponse> GetByIdAsync(int id, string userId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<BookingResponse> CreateAsync(BookingRequest request, string userId, CancellationToken cancellationToken = default);

    Task<BookingResponse> UpdateAsync(int id, BookingUpdateRequest request, string userId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<BookingResponse> UpdateStatusAsync(int id, string status, string userId, bool isAdmin, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, string userId, bool isAdmin, CancellationToken cancellationToken = default);
}
