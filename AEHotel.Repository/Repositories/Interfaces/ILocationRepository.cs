using AEHotel.Domain.Entities;

namespace AEHotel.Repository.Repositories.Interfaces;

public interface ILocationRepository
{
    Task<IReadOnlyList<Country>> GetCountriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<City>> GetCitiesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<City>> GetCitiesByCountryAsync(int countryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TouristPlace>> GetTouristPlacesByCityAsync(int cityId, CancellationToken cancellationToken = default);

    Task<Country?> GetCountryAsync(int id, CancellationToken cancellationToken = default);

    Task<City?> GetCityAsync(int id, CancellationToken cancellationToken = default);

    Task<TouristPlace?> GetTouristPlaceAsync(int id, CancellationToken cancellationToken = default);
}
