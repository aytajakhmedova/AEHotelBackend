using AEHotel.Domain.Entities;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.DTOs.Site;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Services.Interfaces;

namespace AEHotel.Service.Services;

public class SiteContentService : ISiteContentService
{
    private readonly ISiteContentRepository _site;
    private readonly IHotelRepository _hotels;
    private readonly IFlightRepository _flights;
    private readonly ITourRepository _tours;

    public SiteContentService(
        ISiteContentRepository site,
        IHotelRepository hotels,
        IFlightRepository flights,
        ITourRepository tours)
    {
        _site = site;
        _hotels = hotels;
        _flights = flights;
        _tours = tours;
    }

    public async Task<SiteContentResponse> GetPublicAsync(CancellationToken cancellationToken = default)
    {
        var settings = await EnsureSettingsAsync(tracking: false, cancellationToken);
        var menus = (await _site.GetMenuItemsAsync(activeOnly: true, cancellationToken))
            .Where(item => item.Url != "/taxis" && !string.Equals(item.IconKey, "taxi", StringComparison.OrdinalIgnoreCase))
            .ToList();
        return new SiteContentResponse
        {
            Settings = MapSettings(settings),
            Menus = new SiteMenusResponse
            {
                Discover = menus.Where(item => item.Placement == "discover").Select(MapMenu).ToList(),
                Pages = menus.Where(item => item.Placement == "pages").Select(MapMenu).ToList(),
                Quick = menus.Where(item => item.Placement == "quick").Select(MapMenu).ToList(),
                Footer = menus.Where(item => item.Placement == "footer").Select(MapMenu).ToList()
            },
            Slides = (await _site.GetSlidesAsync(activeOnly: true, cancellationToken)).Select(MapSlide).ToList(),
            Advantages = (await _site.GetAdvantagesAsync(activeOnly: true, cancellationToken)).Select(MapAdvantage).ToList()
        };
    }

    public async Task<IReadOnlyList<SiteCityResponse>> GetCitiesAsync(CancellationToken cancellationToken = default)
    {
        var cities = new Dictionary<string, SiteCityResponse>(StringComparer.OrdinalIgnoreCase);
        foreach (var hotel in await _hotels.GetAllAsync(cancellationToken))
        {
            AddCity(cities, hotel.City, hotel.Country);
        }

        foreach (var flight in await _flights.GetAllAsync(cancellationToken))
        {
            AddCity(cities, flight.OriginCity, flight.OriginCountry);
            AddCity(cities, flight.DestinationCity, flight.DestinationCountry);
        }

        foreach (var tour in await _tours.GetAllAsync(cancellationToken))
        {
            var parts = tour.Destination.Split(',', 2, StringSplitOptions.TrimEntries);
            AddCity(cities, parts[0], parts.Length > 1 ? parts[1] : string.Empty);
        }

        return cities.Values.OrderBy(item => item.Name).ToList();
    }

    public async Task<SiteSettingsResponse> UpdateSettingsAsync(SiteSettingsRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SiteName))
        {
            throw new ValidationException("Site name is required.");
        }

        var settings = await EnsureSettingsAsync(tracking: true, cancellationToken);
        ApplySettings(settings, request);
        await _site.SaveChangesAsync(cancellationToken);
        return MapSettings(settings);
    }

    public async Task<IReadOnlyList<SiteMenuItemResponse>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        return (await _site.GetMenuItemsAsync(activeOnly: false, cancellationToken))
            .Where(item => item.Url != "/taxis" && !string.Equals(item.IconKey, "taxi", StringComparison.OrdinalIgnoreCase))
            .Select(MapMenu)
            .ToList();
    }

    public async Task<SiteMenuItemResponse> CreateMenuItemAsync(SiteMenuItemRequest request, CancellationToken cancellationToken = default)
    {
        ValidateMenu(request);
        var item = new SiteMenuItem();
        ApplyMenu(item, request);
        await _site.AddMenuItemAsync(item, cancellationToken);
        await _site.SaveChangesAsync(cancellationToken);
        return MapMenu(item);
    }

    public async Task<SiteMenuItemResponse> UpdateMenuItemAsync(int id, SiteMenuItemRequest request, CancellationToken cancellationToken = default)
    {
        ValidateMenu(request);
        var item = await _site.GetMenuItemAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Menu item with id {id} was not found.");
        ApplyMenu(item, request);
        await _site.SaveChangesAsync(cancellationToken);
        return MapMenu(item);
    }

    public async Task DeleteMenuItemAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _site.GetMenuItemAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Menu item with id {id} was not found.");
        _site.RemoveMenuItem(item);
        await _site.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SiteSlideResponse>> GetSlidesAsync(CancellationToken cancellationToken = default)
    {
        return (await _site.GetSlidesAsync(activeOnly: false, cancellationToken)).Select(MapSlide).ToList();
    }

    public async Task<SiteSlideResponse> CreateSlideAsync(SiteSlideRequest request, CancellationToken cancellationToken = default)
    {
        ValidateSlide(request);
        var slide = new SiteSlide();
        ApplySlide(slide, request);
        await _site.AddSlideAsync(slide, cancellationToken);
        await _site.SaveChangesAsync(cancellationToken);
        return MapSlide(slide);
    }

    public async Task<SiteSlideResponse> UpdateSlideAsync(int id, SiteSlideRequest request, CancellationToken cancellationToken = default)
    {
        ValidateSlide(request);
        var slide = await _site.GetSlideAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Slide with id {id} was not found.");
        ApplySlide(slide, request);
        await _site.SaveChangesAsync(cancellationToken);
        return MapSlide(slide);
    }

    public async Task DeleteSlideAsync(int id, CancellationToken cancellationToken = default)
    {
        var slide = await _site.GetSlideAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Slide with id {id} was not found.");
        _site.RemoveSlide(slide);
        await _site.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SiteAdvantageResponse>> GetAdvantagesAsync(CancellationToken cancellationToken = default)
    {
        return (await _site.GetAdvantagesAsync(activeOnly: false, cancellationToken)).Select(MapAdvantage).ToList();
    }

    public async Task<SiteAdvantageResponse> CreateAdvantageAsync(SiteAdvantageRequest request, CancellationToken cancellationToken = default)
    {
        ValidateAdvantage(request);
        var advantage = new SiteAdvantage();
        ApplyAdvantage(advantage, request);
        await _site.AddAdvantageAsync(advantage, cancellationToken);
        await _site.SaveChangesAsync(cancellationToken);
        return MapAdvantage(advantage);
    }

    public async Task<SiteAdvantageResponse> UpdateAdvantageAsync(int id, SiteAdvantageRequest request, CancellationToken cancellationToken = default)
    {
        ValidateAdvantage(request);
        var advantage = await _site.GetAdvantageAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Advantage with id {id} was not found.");
        ApplyAdvantage(advantage, request);
        await _site.SaveChangesAsync(cancellationToken);
        return MapAdvantage(advantage);
    }

    public async Task DeleteAdvantageAsync(int id, CancellationToken cancellationToken = default)
    {
        var advantage = await _site.GetAdvantageAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Advantage with id {id} was not found.");
        _site.RemoveAdvantage(advantage);
        await _site.SaveChangesAsync(cancellationToken);
    }

    private async Task<SiteSettings> EnsureSettingsAsync(bool tracking, CancellationToken cancellationToken)
    {
        var settings = await _site.GetSettingsAsync(tracking, cancellationToken);
        if (settings != null)
        {
            return settings;
        }

        settings = new SiteSettings { SiteName = "AEHotel" };
        await _site.AddSettingsAsync(settings, cancellationToken);
        await _site.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static void AddCity(IDictionary<string, SiteCityResponse> cities, string name, string country)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var key = name.Trim();
        if (!cities.ContainsKey(key))
        {
            cities[key] = new SiteCityResponse { Name = key, Country = country.Trim() };
        }
    }

    private static void ValidateMenu(SiteMenuItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Label) || string.IsNullOrWhiteSpace(request.Url))
        {
            throw new ValidationException("Menu label and url are required.");
        }
    }

    private static void ValidateSlide(SiteSlideRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            throw new ValidationException("Slide image is required.");
        }
    }

    private static void ValidateAdvantage(SiteAdvantageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ValidationException("Advantage title is required.");
        }
    }

    private static void ApplyMenu(SiteMenuItem item, SiteMenuItemRequest request)
    {
        item.Label = request.Label.Trim();
        item.Url = request.Url.Trim();
        item.Placement = string.IsNullOrWhiteSpace(request.Placement) ? "footer" : request.Placement.Trim().ToLowerInvariant();
        item.IconKey = request.IconKey?.Trim() ?? string.Empty;
        item.IsRoute = request.IsRoute;
        item.SortOrder = request.SortOrder;
        item.IsActive = request.IsActive;
    }

    private static void ApplySlide(SiteSlide slide, SiteSlideRequest request)
    {
        slide.ImageUrl = request.ImageUrl.Trim();
        slide.Title = request.Title?.Trim() ?? string.Empty;
        slide.Description = request.Description?.Trim() ?? string.Empty;
        slide.SortOrder = request.SortOrder;
        slide.IsActive = request.IsActive;
    }

    private static void ApplyAdvantage(SiteAdvantage advantage, SiteAdvantageRequest request)
    {
        advantage.IconKey = request.IconKey?.Trim() ?? string.Empty;
        advantage.Color = string.IsNullOrWhiteSpace(request.Color) ? "#4ade80" : request.Color.Trim();
        advantage.Title = request.Title.Trim();
        advantage.Description = request.Description?.Trim() ?? string.Empty;
        advantage.SortOrder = request.SortOrder;
        advantage.IsActive = request.IsActive;
    }

    private static void ApplySettings(SiteSettings settings, SiteSettingsRequest request)
    {
        settings.SiteName = request.SiteName.Trim();
        settings.LogoUrl = request.LogoUrl?.Trim() ?? string.Empty;
        settings.HeroKicker = request.HeroKicker?.Trim() ?? string.Empty;
        settings.HeroTitleBefore = request.HeroTitleBefore?.Trim() ?? string.Empty;
        settings.HeroTitleAccent = request.HeroTitleAccent?.Trim() ?? string.Empty;
        settings.HeroTitleAfter = request.HeroTitleAfter?.Trim() ?? string.Empty;
        settings.HeroDescription = request.HeroDescription?.Trim() ?? string.Empty;
        settings.HeroPrimaryButtonText = request.HeroPrimaryButtonText?.Trim() ?? string.Empty;
        settings.HeroPrimaryButtonUrl = request.HeroPrimaryButtonUrl?.Trim() ?? string.Empty;
        settings.HeroSecondaryButtonText = request.HeroSecondaryButtonText?.Trim() ?? string.Empty;
        settings.HeroVideoUrl = request.HeroVideoUrl?.Trim() ?? string.Empty;
        settings.HeroFloatImage1Url = request.HeroFloatImage1Url?.Trim() ?? string.Empty;
        settings.HeroFloatImage2Url = request.HeroFloatImage2Url?.Trim() ?? string.Empty;
        settings.HeroSupportBadge = string.IsNullOrWhiteSpace(request.HeroSupportBadge) ? "7/24" : request.HeroSupportBadge.Trim();
        settings.HeroSupportLabel = request.HeroSupportLabel?.Trim() ?? string.Empty;
        settings.HeroStoryTitle = request.HeroStoryTitle?.Trim() ?? string.Empty;
        settings.HeroStoryText = request.HeroStoryText?.Trim() ?? string.Empty;
        settings.AdvantagesTitle = request.AdvantagesTitle?.Trim() ?? string.Empty;
        settings.AdvantagesDescription = request.AdvantagesDescription?.Trim() ?? string.Empty;
        settings.AdvantagesImageUrl = request.AdvantagesImageUrl?.Trim() ?? string.Empty;
        settings.AdvantagesRating = string.IsNullOrWhiteSpace(request.AdvantagesRating) ? "4.8" : request.AdvantagesRating.Trim();
        settings.GalleryTitle = request.GalleryTitle?.Trim() ?? string.Empty;
        settings.FeaturedEyebrow = request.FeaturedEyebrow?.Trim() ?? string.Empty;
        settings.FeaturedTitle = request.FeaturedTitle?.Trim() ?? string.Empty;
        settings.FooterDescription = request.FooterDescription?.Trim() ?? string.Empty;
        settings.FooterLinksTitle = request.FooterLinksTitle?.Trim() ?? string.Empty;
        settings.FooterHoursTitle = request.FooterHoursTitle?.Trim() ?? string.Empty;
        settings.FooterContactTitle = request.FooterContactTitle?.Trim() ?? string.Empty;
        settings.HoursWeekdays = request.HoursWeekdays?.Trim() ?? string.Empty;
        settings.HoursSaturday = request.HoursSaturday?.Trim() ?? string.Empty;
        settings.HoursSunday = request.HoursSunday?.Trim() ?? string.Empty;
        settings.HoursValue = request.HoursValue?.Trim() ?? string.Empty;
        settings.ContactAddress = request.ContactAddress?.Trim() ?? string.Empty;
        settings.ContactEmail = request.ContactEmail?.Trim() ?? string.Empty;
        settings.ContactPhone = request.ContactPhone?.Trim() ?? string.Empty;
        settings.CopyrightText = request.CopyrightText?.Trim() ?? string.Empty;
        settings.PrivacyUrl = request.PrivacyUrl?.Trim() ?? string.Empty;
        settings.TermsUrl = request.TermsUrl?.Trim() ?? string.Empty;
        settings.LinkedInUrl = request.LinkedInUrl?.Trim() ?? string.Empty;
        settings.TwitterUrl = request.TwitterUrl?.Trim() ?? string.Empty;
        settings.InstagramUrl = request.InstagramUrl?.Trim() ?? string.Empty;
        settings.FacebookUrl = request.FacebookUrl?.Trim() ?? string.Empty;
        settings.NoticeSummer = request.NoticeSummer?.Trim() ?? string.Empty;
        settings.NoticeSupport = request.NoticeSupport?.Trim() ?? string.Empty;
    }

    private static SiteSettingsResponse MapSettings(SiteSettings settings)
    {
        return new SiteSettingsResponse
        {
            Id = settings.Id,
            SiteName = settings.SiteName,
            LogoUrl = settings.LogoUrl,
            HeroKicker = settings.HeroKicker,
            HeroTitleBefore = settings.HeroTitleBefore,
            HeroTitleAccent = settings.HeroTitleAccent,
            HeroTitleAfter = settings.HeroTitleAfter,
            HeroDescription = settings.HeroDescription,
            HeroPrimaryButtonText = settings.HeroPrimaryButtonText,
            HeroPrimaryButtonUrl = settings.HeroPrimaryButtonUrl,
            HeroSecondaryButtonText = settings.HeroSecondaryButtonText,
            HeroVideoUrl = settings.HeroVideoUrl,
            HeroFloatImage1Url = settings.HeroFloatImage1Url,
            HeroFloatImage2Url = settings.HeroFloatImage2Url,
            HeroSupportBadge = settings.HeroSupportBadge,
            HeroSupportLabel = settings.HeroSupportLabel,
            HeroStoryTitle = settings.HeroStoryTitle,
            HeroStoryText = settings.HeroStoryText,
            AdvantagesTitle = settings.AdvantagesTitle,
            AdvantagesDescription = settings.AdvantagesDescription,
            AdvantagesImageUrl = settings.AdvantagesImageUrl,
            AdvantagesRating = settings.AdvantagesRating,
            GalleryTitle = settings.GalleryTitle,
            FeaturedEyebrow = settings.FeaturedEyebrow,
            FeaturedTitle = settings.FeaturedTitle,
            FooterDescription = settings.FooterDescription,
            FooterLinksTitle = settings.FooterLinksTitle,
            FooterHoursTitle = settings.FooterHoursTitle,
            FooterContactTitle = settings.FooterContactTitle,
            HoursWeekdays = settings.HoursWeekdays,
            HoursSaturday = settings.HoursSaturday,
            HoursSunday = settings.HoursSunday,
            HoursValue = settings.HoursValue,
            ContactAddress = settings.ContactAddress,
            ContactEmail = settings.ContactEmail,
            ContactPhone = settings.ContactPhone,
            CopyrightText = settings.CopyrightText,
            PrivacyUrl = settings.PrivacyUrl,
            TermsUrl = settings.TermsUrl,
            LinkedInUrl = settings.LinkedInUrl,
            TwitterUrl = settings.TwitterUrl,
            InstagramUrl = settings.InstagramUrl,
            FacebookUrl = settings.FacebookUrl,
            NoticeSummer = settings.NoticeSummer,
            NoticeSupport = settings.NoticeSupport
        };
    }

    private static SiteMenuItemResponse MapMenu(SiteMenuItem item)
    {
        return new SiteMenuItemResponse
        {
            Id = item.Id,
            Label = item.Label,
            Url = item.Url,
            Placement = item.Placement,
            IconKey = item.IconKey,
            IsRoute = item.IsRoute,
            SortOrder = item.SortOrder,
            IsActive = item.IsActive
        };
    }

    private static SiteSlideResponse MapSlide(SiteSlide slide)
    {
        return new SiteSlideResponse
        {
            Id = slide.Id,
            ImageUrl = slide.ImageUrl,
            Title = slide.Title,
            Description = slide.Description,
            SortOrder = slide.SortOrder,
            IsActive = slide.IsActive
        };
    }

    private static SiteAdvantageResponse MapAdvantage(SiteAdvantage advantage)
    {
        return new SiteAdvantageResponse
        {
            Id = advantage.Id,
            IconKey = advantage.IconKey,
            Color = advantage.Color,
            Title = advantage.Title,
            Description = advantage.Description,
            SortOrder = advantage.SortOrder,
            IsActive = advantage.IsActive
        };
    }
}
