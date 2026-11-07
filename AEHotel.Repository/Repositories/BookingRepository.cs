using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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

    public async Task<IReadOnlyList<Booking>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await Query(tracking: false)
            .Where(booking => booking.UserId == userId)
            .OrderByDescending(booking => booking.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task LockRoomForBookingAsync(int roomId, CancellationToken cancellationToken)
    {
        // Call in the write transaction, in ascending ID order, before checking occupancy.
        if (Context.Database.IsSqlServer())
            await Context.Rooms.FromSqlInterpolated($"SELECT * FROM [Rooms] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {roomId}")
                .AsNoTracking().Select(room => room.Id).ToListAsync(cancellationToken);
    }

    public Task<bool> HasHotelOverlapAsync(int roomId, DateOnly checkIn, DateOnly checkOut, int? excludeBookingId, CancellationToken cancellationToken = default)
    {
        return ActiveHotelStays(checkIn, checkOut, excludeBookingId)
            .AnyAsync(booking => booking.RoomId == roomId || booking.Rooms.Any(stay => stay.RoomId == roomId), cancellationToken);
    }

    public Task<bool> ExistsForRoomAsync(int roomId, CancellationToken cancellationToken = default)
    {
        return Context.Bookings.AnyAsync(booking => booking.RoomId == roomId || booking.Rooms.Any(stay => stay.RoomId == roomId), cancellationToken);
    }

    public Task<bool> ExistsForHotelAsync(int hotelId, CancellationToken cancellationToken = default)
    {
        return Context.Bookings.AnyAsync(booking => booking.HotelId == hotelId, cancellationToken);
    }

    public Task<bool> ExistsForFlightAsync(int flightId, CancellationToken cancellationToken = default)
    {
        return Context.Bookings.AnyAsync(booking => booking.FlightId == flightId, cancellationToken);
    }

    public Task<bool> ExistsForTourAsync(int tourId, CancellationToken cancellationToken = default)
    {
        return Context.Bookings.AnyAsync(booking => booking.TourId == tourId, cancellationToken);
    }

    public async Task<List<int>> GetBusyRoomIdsAsync(DateOnly checkIn, DateOnly checkOut, CancellationToken cancellationToken = default)
    {
        var active = ActiveHotelStays(checkIn, checkOut, null);
        return await active
            .Where(booking => booking.RoomId != null)
            .Select(booking => booking.RoomId!.Value)
            .Union(active.SelectMany(booking => booking.Rooms.Select(stay => stay.RoomId)))
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

    public async Task<int> CountFlightSeatsAsync(int flightId, string cabinClass, CancellationToken cancellationToken = default)
    {
        return await Context.Bookings
            .Where(booking =>
                booking.BookingType == "flight" &&
                booking.FlightId == flightId &&
                booking.CabinClass == cabinClass &&
                booking.Status != "cancelled")
            .SumAsync(booking => (int?)booking.Guests, cancellationToken) ?? 0;
    }

    private IQueryable<Booking> Query(bool tracking)
    {
        IQueryable<Booking> query = Context.Bookings
            .Include(booking => booking.Hotel)
            .Include(booking => booking.Room)
            .Include(booking => booking.Rooms)
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

    public Task<IDbContextTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        return Context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
    }

    public IExecutionStrategy GetExecutionStrategy()
    {
        return Context.Database.CreateExecutionStrategy();
    }
}
