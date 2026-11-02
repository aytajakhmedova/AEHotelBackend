using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using AEHotel.Repository.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Repositories;

public class LocationRepository : ILocationRepository
{
    private readonly AppDbContext _context;

    public LocationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Country>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Countries
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<City>> GetCitiesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Cities
            .AsNoTracking()
            .Include(item => item.Country)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
    }
}
