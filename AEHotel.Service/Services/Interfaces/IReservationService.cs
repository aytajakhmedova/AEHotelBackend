using AEHotel.Service.DTOs.Reservations;

namespace AEHotel.Service.Services.Interfaces;

public interface IReservationService
{
    Task<IReadOnlyList<ReservationResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ReservationResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ReservationResponse> CreateAsync(ReservationRequest request, CancellationToken cancellationToken = default);

    Task<ReservationResponse> UpdateAsync(int id, ReservationRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
