using AEHotel.Domain.Entities;

namespace AEHotel.Repository.Repositories.Interfaces;

public interface IPaymentRepository
{
    Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Payment?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
