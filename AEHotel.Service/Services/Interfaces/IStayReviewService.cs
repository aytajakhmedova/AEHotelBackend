using AEHotel.Service.DTOs.Reviews;

namespace AEHotel.Service.Services.Interfaces;

public interface IStayReviewService
{
    Task<IReadOnlyList<ReviewResponse>> GetByHotelAsync(int hotelId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReviewResponse>> GetMineAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReviewResponse>> GetAllAsync(int? hotelScopeId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForBookingAsync(int bookingId, CancellationToken cancellationToken = default);

    Task<ReviewResponse> CreateAsync(ReviewRequest request, string userId, string guestName, CancellationToken cancellationToken = default);

    Task<ReviewResponse> ApproveAsync(int id, int? hotelScopeId, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, int? hotelScopeId, CancellationToken cancellationToken = default);
}
