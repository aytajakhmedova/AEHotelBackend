namespace AEHotel.Service.DTOs.Site;

public class SiteSettingsRequest
{
    public string SiteName { get; set; } = "AEHotel";

    public string LogoUrl { get; set; } = string.Empty;

    public string HeroKicker { get; set; } = string.Empty;

    public string HeroTitleBefore { get; set; } = string.Empty;

    public string HeroTitleAccent { get; set; } = string.Empty;

    public string HeroTitleAfter { get; set; } = string.Empty;

    public string HeroDescription { get; set; } = string.Empty;

    public string HeroPrimaryButtonText { get; set; } = string.Empty;

    public string HeroPrimaryButtonUrl { get; set; } = string.Empty;

    public string HeroSecondaryButtonText { get; set; } = string.Empty;

    public string HeroVideoUrl { get; set; } = string.Empty;

    public string HeroFloatImage1Url { get; set; } = string.Empty;

    public string HeroFloatImage2Url { get; set; } = string.Empty;

    public string HeroSupportBadge { get; set; } = "7/24";

    public string HeroSupportLabel { get; set; } = string.Empty;

    public string HeroStoryTitle { get; set; } = string.Empty;

    public string HeroStoryText { get; set; } = string.Empty;

    public string AdvantagesTitle { get; set; } = string.Empty;

    public string AdvantagesDescription { get; set; } = string.Empty;

    public string AdvantagesImageUrl { get; set; } = string.Empty;

    public string AdvantagesRating { get; set; } = "4.8";

    public string GalleryTitle { get; set; } = string.Empty;

    public string FeaturedEyebrow { get; set; } = string.Empty;

    public string FeaturedTitle { get; set; } = string.Empty;

    public string FooterDescription { get; set; } = string.Empty;

    public string FooterLinksTitle { get; set; } = string.Empty;

    public string FooterHoursTitle { get; set; } = string.Empty;

    public string FooterContactTitle { get; set; } = string.Empty;

    public string HoursWeekdays { get; set; } = string.Empty;

    public string HoursSaturday { get; set; } = string.Empty;

    public string HoursSunday { get; set; } = string.Empty;

    public string HoursValue { get; set; } = string.Empty;

    public string ContactAddress { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;

    public string CopyrightText { get; set; } = string.Empty;

    public string PrivacyUrl { get; set; } = string.Empty;

    public string TermsUrl { get; set; } = string.Empty;

    public string LinkedInUrl { get; set; } = string.Empty;

    public string TwitterUrl { get; set; } = string.Empty;

    public string InstagramUrl { get; set; } = string.Empty;

    public string FacebookUrl { get; set; } = string.Empty;

    public string NoticeSummer { get; set; } = string.Empty;

    public string NoticeSupport { get; set; } = string.Empty;
}

public class SiteSettingsResponse : SiteSettingsRequest
{
    public int Id { get; set; }
}

public class SiteMenuItemRequest
{
    public string Label { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Placement { get; set; } = "footer";

    public string IconKey { get; set; } = string.Empty;

    public bool IsRoute { get; set; } = true;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public class SiteMenuItemResponse : SiteMenuItemRequest
{
    public int Id { get; set; }
}

public class SiteSlideRequest
{
    public string ImageUrl { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public class SiteSlideResponse : SiteSlideRequest
{
    public int Id { get; set; }
}

public class SiteAdvantageRequest
{
    public string IconKey { get; set; } = string.Empty;

    public string Color { get; set; } = "#4ade80";

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public class SiteAdvantageResponse : SiteAdvantageRequest
{
    public int Id { get; set; }
}

public class SiteMenusResponse
{
    public IReadOnlyList<SiteMenuItemResponse> Discover { get; set; } = [];

    public IReadOnlyList<SiteMenuItemResponse> Pages { get; set; } = [];

    public IReadOnlyList<SiteMenuItemResponse> Quick { get; set; } = [];

    public IReadOnlyList<SiteMenuItemResponse> Footer { get; set; } = [];
}

public class SiteContentResponse
{
    public SiteSettingsResponse Settings { get; set; } = new();

    public SiteMenusResponse Menus { get; set; } = new();

    public IReadOnlyList<SiteSlideResponse> Slides { get; set; } = [];

    public IReadOnlyList<SiteAdvantageResponse> Advantages { get; set; } = [];
}

public class SiteCityResponse
{
    public string Name { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;
}

public class UploadResponse
{
    public string Url { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
}
