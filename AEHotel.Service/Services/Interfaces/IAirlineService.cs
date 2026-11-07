using AEHotel.Service.DTOs.Airlines;

namespace AEHotel.Service.Services.Interfaces;

public interface IAirlineService
{
    Task<IReadOnlyList<AirlineResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the stored name of the airline, or null if there is no such airline.</summary>
    Task<string?> FindNameAsync(string name, CancellationToken cancellationToken = default);

    Task<AirlineResponse> CreateAsync(AirlineRequest request, CancellationToken cancellationToken = default);

    Task<AirlineResponse> UpdateAsync(int id, AirlineRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
