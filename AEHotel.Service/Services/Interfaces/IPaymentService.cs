using AEHotel.Service.DTOs.Payments;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.Exceptions;
namespace AEHotel.Service.Services.Interfaces;

public interface IPaymentService
{
    Task<IReadOnlyList<PaymentResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PaymentResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PaymentResponse> UpdateStatusAsync(int id, string status, CancellationToken cancellationToken = default);
}
