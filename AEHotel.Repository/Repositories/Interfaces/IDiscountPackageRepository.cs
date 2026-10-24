using AEHotel.Domain.Entities;

namespace AEHotel.Repository.Repositories.Interfaces;

public interface IDiscountPackageRepository : IBaseRepository<DiscountPackage>
{
    Task<IReadOnlyList<DiscountPackage>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<DiscountPackage?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken = default);

    Task<DiscountPackage?> GetActiveByComboAsync(string combo, DateTime utcNow, CancellationToken cancellationToken = default);
}
