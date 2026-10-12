using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Repositories;

public class HotelRepository : BaseRepository<Hotel>, IHotelRepository
{
    public HotelRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Hotel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Hotels
            .AsNoTracking()
            .Include(hotel => hotel.Rooms)
            .OrderBy(hotel => hotel.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Hotel?> GetByIdAsync(int id, bool includeRooms, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<Hotel> query = Context.Hotels;

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
        return Context.Hotels.AnyAsync(hotel => hotel.Id == id, cancellationToken);
    }

    public Task<string?> GetTitleAsync(int id, CancellationToken cancellationToken = default)
    {
        return Context.Hotels
            .Where(hotel => hotel.Id == id)
            .Select(hotel => hotel.Title)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasReservationsAsync(int hotelId, CancellationToken cancellationToken = default)
    {
        return Context.Reservations.AnyAsync(reservation => reservation.HotelId == hotelId, cancellationToken);
    }

    public async Task LoadRoomsAsync(Hotel hotel, CancellationToken cancellationToken = default)
    {
        await Context.Entry(hotel).Collection(entry => entry.Rooms).LoadAsync(cancellationToken);
    }
}
