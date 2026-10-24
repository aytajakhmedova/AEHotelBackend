using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using AEHotel.Repository.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Repositories;

public class DiscountPackageRepository : BaseRepository<DiscountPackage>, IDiscountPackageRepository
{
    public DiscountPackageRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<DiscountPackage>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Context.DiscountPackages
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<DiscountPackage?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<DiscountPackage> query = Context.DiscountPackages;
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<DiscountPackage?> GetActiveByComboAsync(string combo, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        return Context.DiscountPackages
            .AsNoTracking()
            .Where(item => item.IsActive && item.Combo == combo)
            .Where(item => !item.StartsAt.HasValue || item.StartsAt <= utcNow)
            .Where(item => !item.EndsAt.HasValue || item.EndsAt >= utcNow)
            .OrderByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
