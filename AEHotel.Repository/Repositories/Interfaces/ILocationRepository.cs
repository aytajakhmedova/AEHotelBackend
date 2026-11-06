using AEHotel.Domain.Entities;

using Microsoft.EntityFrameworkCore.Storage;

namespace AEHotel.Repository.Repositories.Interfaces;

public interface ILocationRepository
{
    Task<IReadOnlyList<Country>> GetCountriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<City>> GetCitiesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<City>> GetCitiesByCountryAsync(int countryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TouristPlace>> GetTouristPlacesByCityAsync(int cityId, CancellationToken cancellationToken = default);

    Task<Country?> GetCountryAsync(int id, CancellationToken cancellationToken = default, bool tracking = false);

    Task<City?> GetCityAsync(int id, CancellationToken cancellationToken = default, bool tracking = false);

    Task<TouristPlace?> GetTouristPlaceAsync(int id, CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginCatalogWriteAsync(CancellationToken cancellationToken);
    Task<(List<Hotel> Hotels, List<Flight> Flights, List<Tour> Tours)> GetUsageAsync(Country country, City? city, CancellationToken cancellationToken);
    void Add(Country country);
    void Add(City city);
    void Remove(Country country);
    void Remove(City city);
    void Remove(Hotel hotel);
    void Remove(Flight flight);
    void Remove(Tour tour);
    Task DeleteHotelWithRelatedDataAsync(Hotel hotel, CancellationToken cancellationToken);
    Task DeleteCountryCascadeAsync(int countryId, CancellationToken cancellationToken);
    Task DeleteCityCascadeAsync(int cityId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
