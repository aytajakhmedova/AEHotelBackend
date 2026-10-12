using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Rooms;

public class RoomRepository : IRoomRepository
{
    private readonly AppDbContext _context;

    public RoomRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Rooms
            .AsNoTracking()
            .OrderBy(room => room.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Room>> GetByHotelIdAsync(int hotelId, CancellationToken cancellationToken = default)
    {
        return await _context.Rooms
            .AsNoTracking()
            .Where(room => room.HotelId == hotelId)
            .OrderBy(room => room.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Room?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<Room> query = _context.Rooms;

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(room => room.Id == id, cancellationToken);
    }

    public Task<bool> HasReservationsAsync(int roomId, CancellationToken cancellationToken = default)
    {
        return _context.Reservations.AnyAsync(reservation => reservation.RoomId == roomId, cancellationToken);
    }

    public async Task AddAsync(Room room, CancellationToken cancellationToken = default)
    {
        await _context.Rooms.AddAsync(room, cancellationToken);
    }

    public void Remove(Room room)
    {
        _context.Rooms.Remove(room);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
