using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Service.Services;

public class FavoriteService : IFavoriteService
{
    private readonly AppDbContext _db;

    public FavoriteService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<int>> GetHotelIdsAsync(string userId, CancellationToken cancellationToken = default) =>
        await _db.FavoriteHotels.AsNoTracking()
            .Where(item => item.UserId == userId && !item.Hotel!.IsDeleted)
            .OrderByDescending(item => item.Id)
            .Select(item => item.HotelId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(string userId, int hotelId, CancellationToken cancellationToken = default)
    {
        var hotelExists = await _db.Hotels.AnyAsync(hotel => hotel.Id == hotelId && !hotel.IsDeleted, cancellationToken);
        if (!hotelExists)
        {
            throw new NotFoundException($"Hotel with id {hotelId} was not found.");
        }

        if (await _db.FavoriteHotels.AnyAsync(item => item.UserId == userId && item.HotelId == hotelId, cancellationToken))
        {
            return;
        }

        _db.FavoriteHotels.Add(new FavoriteHotel { UserId = userId, HotelId = hotelId });
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // a double click added it at the same moment: it is a favourite already
        }
    }

    public async Task RemoveAsync(string userId, int hotelId, CancellationToken cancellationToken = default)
    {
        var item = await _db.FavoriteHotels.FirstOrDefaultAsync(entry => entry.UserId == userId && entry.HotelId == hotelId, cancellationToken);
        if (item != null)
        {
            _db.FavoriteHotels.Remove(item);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
