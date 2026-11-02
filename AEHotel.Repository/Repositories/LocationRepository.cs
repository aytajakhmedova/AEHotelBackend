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

    public async Task<IReadOnlyList<City>> GetCitiesByCountryAsync(int countryId, CancellationToken cancellationToken = default)
    {
        return await _context.Cities
            .AsNoTracking()
            .Where(item => item.CountryId == countryId)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TouristPlace>> GetTouristPlacesByCityAsync(int cityId, CancellationToken cancellationToken = default)
    {
        return await _context.TouristPlaces
            .AsNoTracking()
            .Where(item => item.CityId == cityId)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Country?> GetCountryAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Countries.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<City?> GetCityAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Cities
            .AsNoTracking()
            .Include(item => item.Country)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<TouristPlace?> GetTouristPlaceAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.TouristPlaces.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }
}
