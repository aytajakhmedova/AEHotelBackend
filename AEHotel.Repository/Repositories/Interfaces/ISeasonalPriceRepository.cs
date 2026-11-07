using AEHotel.Domain.Entities;

namespace AEHotel.Repository.Repositories.Interfaces;

public interface ISeasonalPriceRepository : IBaseRepository<SeasonalPrice>
{
    Task<IReadOnlyList<SeasonalPrice>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SeasonalPrice?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string country, int month, int? excludeId, CancellationToken cancellationToken = default);
}
