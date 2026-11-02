using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

public class SiteSettings : BaseEntity
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

    public string HotelsPageTitle { get; set; } = "Otelləri Kəşf Et";

    public string HotelsPageSubtitle { get; set; } = "Lüks qonaq evlərindən tutmuş büdcəli seçimlərə qədər";

    public string HotelsPageBannerUrl { get; set; } = string.Empty;

    public string HotelsPageVideoUrl { get; set; } = string.Empty;

    public string FlightsPageTitle { get; set; } = "Uçuşları Kəşf Et";

    public string FlightsPageSubtitle { get; set; } = "Dünya üzrə ən yaxşı qiymətlərlə uçuşlar";

    public string FlightsPageBannerUrl { get; set; } = string.Empty;

    public string FlightsPageVideoUrl { get; set; } = string.Empty;

    public string ToursPageTitle { get; set; } = "Turları Kəşf Et";

    public string ToursPageSubtitle { get; set; } = "Azərbaycanın gözəl yerlərini kəşf edin";

    public string ToursPageBannerUrl { get; set; } = string.Empty;

    public string ToursPageVideoUrl { get; set; } = string.Empty;
}
