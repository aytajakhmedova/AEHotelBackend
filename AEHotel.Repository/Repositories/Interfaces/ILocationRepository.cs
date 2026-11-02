using AEHotel.Domain.Entities;

namespace AEHotel.Repository.Repositories.Interfaces;

public interface ILocationRepository
{
    Task<IReadOnlyList<Country>> GetCountriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<City>> GetCitiesAsync(CancellationToken cancellationToken = default);
}
