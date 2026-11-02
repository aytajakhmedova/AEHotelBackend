using AEHotel.Service.DTOs.Site;

namespace AEHotel.Service.Services.Interfaces;

public interface ILocationService
{
    Task<IReadOnlyList<CountryOptionResponse>> GetCountriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CityOptionResponse>> GetCitiesAsync(int countryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TouristPlaceOptionResponse>> GetTouristPlacesAsync(int cityId, CancellationToken cancellationToken = default);
}
