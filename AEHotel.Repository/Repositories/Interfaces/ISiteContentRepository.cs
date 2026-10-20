using AEHotel.Domain.Entities;

namespace AEHotel.Repository.Repositories.Interfaces;

public interface ISiteContentRepository
{
    Task<SiteSettings?> GetSettingsAsync(bool tracking, CancellationToken cancellationToken = default);

    Task AddSettingsAsync(SiteSettings settings, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SiteMenuItem>> GetMenuItemsAsync(bool activeOnly, CancellationToken cancellationToken = default);

    Task<SiteMenuItem?> GetMenuItemAsync(int id, bool tracking, CancellationToken cancellationToken = default);

    Task AddMenuItemAsync(SiteMenuItem item, CancellationToken cancellationToken = default);

    void RemoveMenuItem(SiteMenuItem item);

    Task<IReadOnlyList<SiteSlide>> GetSlidesAsync(bool activeOnly, CancellationToken cancellationToken = default);

    Task<SiteSlide?> GetSlideAsync(int id, bool tracking, CancellationToken cancellationToken = default);

    Task AddSlideAsync(SiteSlide slide, CancellationToken cancellationToken = default);

    void RemoveSlide(SiteSlide slide);

    Task<IReadOnlyList<SiteAdvantage>> GetAdvantagesAsync(bool activeOnly, CancellationToken cancellationToken = default);

    Task<SiteAdvantage?> GetAdvantageAsync(int id, bool tracking, CancellationToken cancellationToken = default);

    Task AddAdvantageAsync(SiteAdvantage advantage, CancellationToken cancellationToken = default);

    void RemoveAdvantage(SiteAdvantage advantage);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
