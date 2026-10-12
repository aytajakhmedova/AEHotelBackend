using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Reservations;

public class ReservationRepository : IReservationRepository
{
    private readonly AppDbContext _context;

    public ReservationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Reservation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Reservations
            .AsNoTracking()
            .Include(reservation => reservation.Hotel)
            .Include(reservation => reservation.Room)
            .OrderByDescending(reservation => reservation.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Reservation?> GetByIdAsync(int id, bool tracking, bool includeDetails, CancellationToken cancellationToken = default)
    {
        IQueryable<Reservation> query = _context.Reservations;

        if (includeDetails)
        {
            query = query
                .Include(reservation => reservation.Hotel)
                .Include(reservation => reservation.Room);
        }

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(reservation => reservation.Id == id, cancellationToken);
    }

    public Task<bool> HasOverlapAsync(
        int roomId,
        DateOnly checkIn,
        DateOnly checkOut,
        int? excludeReservationId,
        CancellationToken cancellationToken = default)
    {
        return _context.Reservations.AnyAsync(reservation =>
            reservation.RoomId == roomId &&
            reservation.Status != "cancelled" &&
            (excludeReservationId == null || reservation.Id != excludeReservationId.Value) &&
            reservation.CheckIn < checkOut &&
            checkIn < reservation.CheckOut,
            cancellationToken);
    }

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        await _context.Reservations.AddAsync(reservation, cancellationToken);
    }

    public void Remove(Reservation reservation)
    {
        _context.Reservations.Remove(reservation);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
