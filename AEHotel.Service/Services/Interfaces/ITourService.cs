using AEHotel.Service.DTOs.Tours;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Helpers;
namespace AEHotel.Service.Services.Interfaces;

public interface ITourService
{
    Task<IReadOnlyList<TourResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TourResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<TourResponse> CreateAsync(TourRequest request, CancellationToken cancellationToken = default);

    Task<TourResponse> UpdateAsync(int id, TourRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
