using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Repositories;

public class RoomRepository : BaseRepository<Room>, IRoomRepository
{
    public RoomRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Rooms
            .AsNoTracking()
            .OrderBy(room => room.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Room>> GetByHotelIdAsync(int hotelId, CancellationToken cancellationToken = default)
    {
        return await Context.Rooms
            .AsNoTracking()
            .Where(room => room.HotelId == hotelId)
            .OrderBy(room => room.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Room?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<Room> query = Context.Rooms;

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(room => room.Id == id, cancellationToken);
    }

    public Task<bool> HasReservationsAsync(int roomId, CancellationToken cancellationToken = default)
    {
        return Context.Reservations.AnyAsync(reservation => reservation.RoomId == roomId, cancellationToken);
    }
}
