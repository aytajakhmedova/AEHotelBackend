using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using AEHotel.Repository.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Repositories;

public class SeasonalPriceRepository : BaseRepository<SeasonalPrice>, ISeasonalPriceRepository
{
    public SeasonalPriceRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<SeasonalPrice>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Context.SeasonalPrices.AsNoTracking()
            .OrderBy(item => item.Country).ThenBy(item => item.Month)
            .ToListAsync(cancellationToken);
    }

    public Task<SeasonalPrice?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<SeasonalPrice> query = Context.SeasonalPrices;
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(string country, int month, int? excludeId, CancellationToken cancellationToken = default)
    {
        return Context.SeasonalPrices.AnyAsync(
            item => item.Country == country && item.Month == month && (!excludeId.HasValue || item.Id != excludeId.Value),
            cancellationToken);
    }
}
