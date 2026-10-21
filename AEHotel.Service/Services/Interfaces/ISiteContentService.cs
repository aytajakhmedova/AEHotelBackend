using AEHotel.Service.DTOs.Site;

namespace AEHotel.Service.Services.Interfaces;

public interface ISiteContentService
{
    Task<SiteContentResponse> GetPublicAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SiteCityResponse>> GetCitiesAsync(CancellationToken cancellationToken = default);

    Task<SiteSettingsResponse> UpdateSettingsAsync(SiteSettingsRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SiteMenuItemResponse>> GetMenuItemsAsync(CancellationToken cancellationToken = default);

    Task<SiteMenuItemResponse> CreateMenuItemAsync(SiteMenuItemRequest request, CancellationToken cancellationToken = default);

    Task<SiteMenuItemResponse> UpdateMenuItemAsync(int id, SiteMenuItemRequest request, CancellationToken cancellationToken = default);

    Task DeleteMenuItemAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SiteSlideResponse>> GetSlidesAsync(CancellationToken cancellationToken = default);

    Task<SiteSlideResponse> CreateSlideAsync(SiteSlideRequest request, CancellationToken cancellationToken = default);

    Task<SiteSlideResponse> UpdateSlideAsync(int id, SiteSlideRequest request, CancellationToken cancellationToken = default);

    Task DeleteSlideAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SiteAdvantageResponse>> GetAdvantagesAsync(CancellationToken cancellationToken = default);

    Task<SiteAdvantageResponse> CreateAdvantageAsync(SiteAdvantageRequest request, CancellationToken cancellationToken = default);

    Task<SiteAdvantageResponse> UpdateAdvantageAsync(int id, SiteAdvantageRequest request, CancellationToken cancellationToken = default);

    Task DeleteAdvantageAsync(int id, CancellationToken cancellationToken = default);
}
