using AEHotel.Service.DTOs.Reviews;

namespace AEHotel.Service.Services.Interfaces;

public interface IStayReviewService
{
    Task<IReadOnlyList<ReviewResponse>> GetByHotelAsync(int hotelId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForBookingAsync(int bookingId, CancellationToken cancellationToken = default);

    Task<ReviewResponse> CreateAsync(ReviewRequest request, string userId, string guestName, CancellationToken cancellationToken = default);
}
