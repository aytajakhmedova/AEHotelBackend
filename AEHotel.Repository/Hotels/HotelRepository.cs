using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Hotels;

public class HotelRepository : IHotelRepository
{
    private readonly AppDbContext _context;

    public HotelRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Hotel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Hotels
            .AsNoTracking()
            .Include(hotel => hotel.Rooms)
            .OrderBy(hotel => hotel.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Hotel?> GetByIdAsync(int id, bool includeRooms, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<Hotel> query = _context.Hotels;

        if (includeRooms)
        {
            query = query.Include(hotel => hotel.Rooms);
        }

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(hotel => hotel.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Hotels.AnyAsync(hotel => hotel.Id == id, cancellationToken);
    }

    public Task<string?> GetTitleAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Hotels
            .Where(hotel => hotel.Id == id)
            .Select(hotel => hotel.Title)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasReservationsAsync(int hotelId, CancellationToken cancellationToken = default)
    {
        return _context.Reservations.AnyAsync(reservation => reservation.HotelId == hotelId, cancellationToken);
    }

    public async Task AddAsync(Hotel hotel, CancellationToken cancellationToken = default)
    {
        await _context.Hotels.AddAsync(hotel, cancellationToken);
    }

    public void Remove(Hotel hotel)
    {
        _context.Hotels.Remove(hotel);
    }

    public async Task LoadRoomsAsync(Hotel hotel, CancellationToken cancellationToken = default)
    {
        await _context.Entry(hotel).Collection(entry => entry.Rooms).LoadAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
