using AEHotel.Service.DTOs.Flights;

namespace AEHotel.Service.Services.Interfaces;

public interface IFlightService
{
    Task<IReadOnlyList<FlightResponse>> GetAllAsync(string? origin, string? destination, DateOnly? date, int? passengers, string? cabin, string? sort, CancellationToken cancellationToken = default);

    Task<FlightResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<FlightResponse> CreateAsync(FlightRequest request, CancellationToken cancellationToken = default);

    Task<FlightResponse> UpdateAsync(int id, FlightRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
