using AEHotel.Service.DTOs.Tours;

namespace AEHotel.Service.Services.Interfaces;

public interface ITourService
{
    Task<IReadOnlyList<TourResponse>> GetAllAsync(string? destination = null, DateOnly? date = null, int? guests = null, CancellationToken cancellationToken = default);

    Task<TourResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<TourResponse> CreateAsync(TourRequest request, CancellationToken cancellationToken = default);

    Task<TourResponse> UpdateAsync(int id, TourRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
