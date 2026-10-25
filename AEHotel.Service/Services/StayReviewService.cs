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
            .Where(item => item.HotelId == hotelId)
            .OrderByDescending(item => item.CreatedAt)
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
            CreatedAt = DateTime.UtcNow
        };

        _db.StayReviews.Add(review);
        await _db.SaveChangesAsync(cancellationToken);

        var hotel = await _hotels.GetByIdAsync(request.HotelId, includeRooms: false, tracking: true, cancellationToken);
        if (hotel != null)
        {
            var ratings = await _db.StayReviews.AsNoTracking()
                .Where(item => item.HotelId == request.HotelId)
                .Select(item => item.Rating)
                .ToListAsync(cancellationToken);
            hotel.ReviewCount = ratings.Count;
            hotel.Rating = ratings.Count == 0 ? 0 : Math.Round((decimal)ratings.Average(), 1);
            await _hotels.SaveChangesAsync(cancellationToken);
        }

        return Map(review);
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
            CreatedAt = review.CreatedAt
        };
    }
}
