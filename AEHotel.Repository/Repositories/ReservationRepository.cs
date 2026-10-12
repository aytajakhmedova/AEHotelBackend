using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Repositories;

public class ReservationRepository : BaseRepository<Reservation>, IReservationRepository
{
    public ReservationRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Reservation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Reservations
            .AsNoTracking()
            .Include(reservation => reservation.Hotel)
            .Include(reservation => reservation.Room)
            .OrderByDescending(reservation => reservation.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Reservation?> GetByIdAsync(int id, bool tracking, bool includeDetails, CancellationToken cancellationToken = default)
    {
        IQueryable<Reservation> query = Context.Reservations;

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
        return Context.Reservations.AnyAsync(reservation =>
            reservation.RoomId == roomId &&
            reservation.Status != "cancelled" &&
            (excludeReservationId == null || reservation.Id != excludeReservationId.Value) &&
            reservation.CheckIn < checkOut &&
            checkIn < reservation.CheckOut,
            cancellationToken);
    }
}
