using AEHotel.Service.DTOs.Site;

namespace AEHotel.Service.Services.Interfaces;

public interface ILocationService
{
    Task<IReadOnlyList<CountryOptionResponse>> GetCountriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CityOptionResponse>> GetCitiesAsync(int? countryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TouristPlaceOptionResponse>> GetTouristPlacesAsync(int cityId, CancellationToken cancellationToken = default);

    Task<CountryOptionResponse> SaveCountryAsync(int? id, CountryRequest request, CancellationToken cancellationToken);
    Task<CityOptionResponse> SaveCityAsync(int? id, CityRequest request, CancellationToken cancellationToken);
    Task DeleteCountryAsync(int id, CancellationToken cancellationToken);
    Task DeleteCityAsync(int id, CancellationToken cancellationToken);
}
