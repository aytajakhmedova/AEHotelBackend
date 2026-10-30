using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.DTOs.Reviews;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Service.Services;

public class StayReviewService : IStayReviewService
{
    private readonly AppDbContext _db;
    private readonly IBookingRepository _bookings;
    private readonly IHotelRepository _hotels;

    public StayReviewService(AppDbContext db, IBookingRepository bookings, IHotelRepository hotels)
    {
        _db = db;
        _bookings = bookings;
        _hotels = hotels;
    }

    public async Task<IReadOnlyList<ReviewResponse>> GetByHotelAsync(int hotelId, CancellationToken cancellationToken = default)
    {
        var items = await _db.StayReviews.AsNoTracking()
            .Include(item => item.Hotel)
            .Where(item => item.HotelId == hotelId && item.IsApproved)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ReviewResponse>> GetMineAsync(string userId, CancellationToken cancellationToken = default)
    {
        var items = await _db.StayReviews.AsNoTracking()
            .Include(item => item.Hotel)
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ReviewResponse>> GetAllAsync(int? hotelScopeId, CancellationToken cancellationToken = default)
    {
        var query = _db.StayReviews.AsNoTracking().Include(item => item.Hotel).AsQueryable();
        if (hotelScopeId.HasValue)
        {
            query = hotelScopeId.Value == 0
                ? query.Where(_ => false)
                : query.Where(item => item.HotelId == hotelScopeId.Value);
        }

        var items = await query.OrderBy(item => item.IsApproved).ThenByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public Task<bool> ExistsForBookingAsync(int bookingId, CancellationToken cancellationToken = default)
    {
        return _db.StayReviews.AsNoTracking().AnyAsync(item => item.BookingId == bookingId, cancellationToken);
    }

    public async Task<ReviewResponse> CreateAsync(ReviewRequest request, string userId, string guestName, CancellationToken cancellationToken = default)
    {
        if (request.Rating is < 1 or > 5)
        {
            throw new ValidationException("Rating must be between 1 and 5.");
        }

        if (string.IsNullOrWhiteSpace(request.Comment) || request.Comment.Trim().Length < 10)
        {
            throw new ValidationException("Review must be at least 10 characters.");
        }

        var booking = await _bookings.GetByIdAsync(request.BookingId, tracking: false, cancellationToken)
            ?? throw new NotFoundException("Booking was not found.");

        if (booking.UserId != userId)
        {
            throw new ForbiddenException("You can only review your own stay.");
        }

        if (!string.Equals(booking.Status, "checked-out", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Review is allowed only after check-out.");
        }

        if (booking.HotelId != request.HotelId)
        {
            throw new ValidationException("This stay does not belong to the selected hotel.");
        }

        if (await ExistsForBookingAsync(request.BookingId, cancellationToken))
        {
            throw new ValidationException("This stay already has a review.");
        }

        var review = new StayReview
        {
            HotelId = request.HotelId,
            BookingId = request.BookingId,
            UserId = userId,
            GuestName = string.IsNullOrWhiteSpace(guestName) ? booking.GuestName : guestName,
            Rating = request.Rating,
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Qalma rəyi" : request.Title.Trim(),
            Comment = request.Comment.Trim(),
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.StayReviews.Add(review);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(review);
    }

    public async Task<ReviewResponse> ApproveAsync(int id, int? hotelScopeId, CancellationToken cancellationToken = default)
    {
        var review = await RequireReviewAsync(id, hotelScopeId, cancellationToken);
        review.IsApproved = true;
        await _db.SaveChangesAsync(cancellationToken);
        await RecalcHotelAsync(review.HotelId, cancellationToken);
        return Map(review);
    }

    public async Task DeleteAsync(int id, int? hotelScopeId, CancellationToken cancellationToken = default)
    {
        var review = await RequireReviewAsync(id, hotelScopeId, cancellationToken);
        var hotelId = review.HotelId;
        _db.StayReviews.Remove(review);
        await _db.SaveChangesAsync(cancellationToken);
        await RecalcHotelAsync(hotelId, cancellationToken);
    }

    private async Task<StayReview> RequireReviewAsync(int id, int? hotelScopeId, CancellationToken cancellationToken)
    {
        var review = await _db.StayReviews.Include(item => item.Hotel)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Rəy tapılmadı.");

        if (hotelScopeId.HasValue && (hotelScopeId.Value == 0 || review.HotelId != hotelScopeId.Value))
        {
            throw new ForbiddenException("Bu rəy sizin otelə aid deyil.");
        }

        return review;
    }

    private async Task RecalcHotelAsync(int hotelId, CancellationToken cancellationToken)
    {
        var hotel = await _hotels.GetByIdAsync(hotelId, includeRooms: false, tracking: true, cancellationToken);
        if (hotel == null)
        {
            return;
        }

        var ratings = await _db.StayReviews.AsNoTracking()
            .Where(item => item.HotelId == hotelId && item.IsApproved)
            .Select(item => item.Rating)
            .ToListAsync(cancellationToken);
        hotel.ReviewCount = ratings.Count;
        hotel.Rating = ratings.Count == 0 ? 0 : Math.Round((decimal)ratings.Average(), 1);
        await _hotels.SaveChangesAsync(cancellationToken);
    }

    private static ReviewResponse Map(StayReview review)
    {
        return new ReviewResponse
        {
            Id = review.Id,
            HotelId = review.HotelId,
            BookingId = review.BookingId,
            GuestName = review.GuestName,
            Rating = review.Rating,
            Title = review.Title,
            Comment = review.Comment,
            IsApproved = review.IsApproved,
            HotelName = review.Hotel?.Title ?? string.Empty,
            CreatedAt = review.CreatedAt
        };
    }
}
