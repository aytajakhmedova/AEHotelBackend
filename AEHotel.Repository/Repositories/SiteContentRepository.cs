using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using AEHotel.Repository.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Repositories;

public class SiteContentRepository : ISiteContentRepository
{
    private readonly AppDbContext _context;

    public SiteContentRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<SiteSettings?> GetSettingsAsync(bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<SiteSettings> query = _context.SiteSettings;
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.OrderBy(item => item.Id).FirstOrDefaultAsync(cancellationToken);
    }

    public Task AddSettingsAsync(SiteSettings settings, CancellationToken cancellationToken = default)
    {
        return _context.SiteSettings.AddAsync(settings, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<SiteMenuItem>> GetMenuItemsAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = _context.SiteMenuItems.AsNoTracking().AsQueryable();
        if (activeOnly)
        {
            query = query.Where(item => item.IsActive);
        }

        return await query.OrderBy(item => item.SortOrder).ThenBy(item => item.Id).ToListAsync(cancellationToken);
    }

    public Task<SiteMenuItem?> GetMenuItemAsync(int id, bool tracking, CancellationToken cancellationToken = default)
    {
        return Query<SiteMenuItem>(tracking).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task AddMenuItemAsync(SiteMenuItem item, CancellationToken cancellationToken = default)
    {
        return _context.SiteMenuItems.AddAsync(item, cancellationToken).AsTask();
    }

    public void RemoveMenuItem(SiteMenuItem item) => _context.SiteMenuItems.Remove(item);

    public async Task<IReadOnlyList<SiteSlide>> GetSlidesAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = _context.SiteSlides.AsNoTracking().AsQueryable();
        if (activeOnly)
        {
            query = query.Where(item => item.IsActive);
        }

        return await query.OrderBy(item => item.SortOrder).ThenBy(item => item.Id).ToListAsync(cancellationToken);
    }

    public Task<SiteSlide?> GetSlideAsync(int id, bool tracking, CancellationToken cancellationToken = default)
    {
        return Query<SiteSlide>(tracking).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task AddSlideAsync(SiteSlide slide, CancellationToken cancellationToken = default)
    {
        return _context.SiteSlides.AddAsync(slide, cancellationToken).AsTask();
    }

    public void RemoveSlide(SiteSlide slide) => _context.SiteSlides.Remove(slide);

    public async Task<IReadOnlyList<SiteAdvantage>> GetAdvantagesAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = _context.SiteAdvantages.AsNoTracking().AsQueryable();
        if (activeOnly)
        {
            query = query.Where(item => item.IsActive);
        }

        return await query.OrderBy(item => item.SortOrder).ThenBy(item => item.Id).ToListAsync(cancellationToken);
    }

    public Task<SiteAdvantage?> GetAdvantageAsync(int id, bool tracking, CancellationToken cancellationToken = default)
    {
        return Query<SiteAdvantage>(tracking).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task AddAdvantageAsync(SiteAdvantage advantage, CancellationToken cancellationToken = default)
    {
        return _context.SiteAdvantages.AddAsync(advantage, cancellationToken).AsTask();
    }

    public void RemoveAdvantage(SiteAdvantage advantage) => _context.SiteAdvantages.Remove(advantage);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<T> Query<T>(bool tracking) where T : class
    {
        IQueryable<T> query = _context.Set<T>();
        return tracking ? query : query.AsNoTracking();
    }
}
