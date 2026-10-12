using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Repositories;

public class BookingRepository : BaseRepository<Booking>, IBookingRepository
{
    public BookingRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Query(tracking: false)
            .OrderByDescending(booking => booking.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Booking?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken = default)
    {
        return Query(tracking).FirstOrDefaultAsync(booking => booking.Id == id, cancellationToken);
    }

    public Task<bool> HasHotelOverlapAsync(int roomId, DateOnly checkIn, DateOnly checkOut, int? excludeBookingId, CancellationToken cancellationToken = default)
    {
        return ActiveHotelStays(checkIn, checkOut, excludeBookingId)
            .AnyAsync(booking => booking.RoomId == roomId, cancellationToken);
    }

    public async Task<List<int>> GetBusyRoomIdsAsync(DateOnly checkIn, DateOnly checkOut, CancellationToken cancellationToken = default)
    {
        return await ActiveHotelStays(checkIn, checkOut, null)
            .Where(booking => booking.RoomId != null)
            .Select(booking => booking.RoomId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountTourGuestsAsync(int tourId, DateOnly travelDate, int? excludeBookingId, CancellationToken cancellationToken = default)
    {
        return await Context.Bookings
            .Where(booking =>
                booking.BookingType == "tour" &&
                booking.TourId == tourId &&
                booking.CheckIn == travelDate &&
                booking.Status != "cancelled" &&
                (excludeBookingId == null || booking.Id != excludeBookingId))
            .SumAsync(booking => (int?)booking.Guests, cancellationToken) ?? 0;
    }

    private IQueryable<Booking> Query(bool tracking)
    {
        IQueryable<Booking> query = Context.Bookings
            .Include(booking => booking.Hotel)
            .Include(booking => booking.Room)
            .Include(booking => booking.Flight)
            .Include(booking => booking.Tour)
            .Include(booking => booking.Payment);

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query;
    }

    private IQueryable<Booking> ActiveHotelStays(DateOnly checkIn, DateOnly checkOut, int? excludeBookingId)
    {
        return Context.Bookings.Where(booking =>
            booking.BookingType == "hotel" &&
            booking.Status != "cancelled" &&
            booking.Status != "checked-out" &&
            booking.CheckIn != null &&
            booking.CheckOut != null &&
            (excludeBookingId == null || booking.Id != excludeBookingId) &&
            booking.CheckIn < checkOut &&
            checkIn < booking.CheckOut);
    }
}
