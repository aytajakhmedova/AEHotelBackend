using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.DTOs.Site;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Services.Interfaces;

namespace AEHotel.Service.Services;

public class LocationService : ILocationService
{
    private readonly ILocationRepository _locations;

    public LocationService(ILocationRepository locations)
    {
        _locations = locations;
    }

    public async Task<IReadOnlyList<CountryOptionResponse>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        return (await _locations.GetCountriesAsync(cancellationToken))
            .Select(country => new CountryOptionResponse { Id = country.Id, Name = country.Name, IsoCode = country.IsoCode })
            .ToList();
    }

    public async Task<IReadOnlyList<CityOptionResponse>> GetCitiesAsync(int countryId, CancellationToken cancellationToken = default)
    {
        if (await _locations.GetCountryAsync(countryId, cancellationToken) == null)
        {
            throw new NotFoundException($"Country with id {countryId} was not found.");
        }

        return (await _locations.GetCitiesByCountryAsync(countryId, cancellationToken))
            .Select(city => new CityOptionResponse { Id = city.Id, Name = city.Name, CountryId = city.CountryId })
            .ToList();
    }

    public async Task<IReadOnlyList<TouristPlaceOptionResponse>> GetTouristPlacesAsync(int cityId, CancellationToken cancellationToken = default)
    {
        if (await _locations.GetCityAsync(cityId, cancellationToken) == null)
        {
            throw new NotFoundException($"City with id {cityId} was not found.");
        }

        return (await _locations.GetTouristPlacesByCityAsync(cityId, cancellationToken))
            .Select(place => new TouristPlaceOptionResponse { Id = place.Id, Name = place.Name, CityId = place.CityId })
            .ToList();
    }
}
