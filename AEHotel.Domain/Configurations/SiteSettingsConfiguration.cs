using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> entity)
    {
        entity.Property(item => item.SiteName).HasMaxLength(120).IsRequired();
        entity.Property(item => item.LogoUrl).HasMaxLength(400);
        entity.Property(item => item.HeroKicker).HasMaxLength(200);
        entity.Property(item => item.HeroTitleBefore).HasMaxLength(200);
        entity.Property(item => item.HeroTitleAccent).HasMaxLength(120);
        entity.Property(item => item.HeroTitleAfter).HasMaxLength(200);
        entity.Property(item => item.HeroDescription).HasMaxLength(2000);
        entity.Property(item => item.HeroPrimaryButtonText).HasMaxLength(80);
        entity.Property(item => item.HeroPrimaryButtonUrl).HasMaxLength(300);
        entity.Property(item => item.HeroSecondaryButtonText).HasMaxLength(80);
        entity.Property(item => item.HeroVideoUrl).HasMaxLength(400);
        entity.Property(item => item.HeroFloatImage1Url).HasMaxLength(400);
        entity.Property(item => item.HeroFloatImage2Url).HasMaxLength(400);
        entity.Property(item => item.HeroSupportBadge).HasMaxLength(40);
        entity.Property(item => item.HeroSupportLabel).HasMaxLength(80);
        entity.Property(item => item.HeroStoryTitle).HasMaxLength(200);
        entity.Property(item => item.HeroStoryText).HasMaxLength(4000);
        entity.Property(item => item.AdvantagesTitle).HasMaxLength(200);
        entity.Property(item => item.AdvantagesDescription).HasMaxLength(2000);
        entity.Property(item => item.AdvantagesImageUrl).HasMaxLength(400);
        entity.Property(item => item.AdvantagesRating).HasMaxLength(20);
        entity.Property(item => item.GalleryTitle).HasMaxLength(200);
        entity.Property(item => item.FeaturedEyebrow).HasMaxLength(120);
        entity.Property(item => item.FeaturedTitle).HasMaxLength(200);
        entity.Property(item => item.FooterDescription).HasMaxLength(2000);
        entity.Property(item => item.FooterLinksTitle).HasMaxLength(80);
        entity.Property(item => item.FooterHoursTitle).HasMaxLength(80);
        entity.Property(item => item.FooterContactTitle).HasMaxLength(80);
        entity.Property(item => item.HoursWeekdays).HasMaxLength(80);
        entity.Property(item => item.HoursSaturday).HasMaxLength(80);
        entity.Property(item => item.HoursSunday).HasMaxLength(80);
        entity.Property(item => item.HoursValue).HasMaxLength(40);
        entity.Property(item => item.ContactAddress).HasMaxLength(300);
        entity.Property(item => item.ContactEmail).HasMaxLength(200);
        entity.Property(item => item.ContactPhone).HasMaxLength(80);
        entity.Property(item => item.CopyrightText).HasMaxLength(200);
        entity.Property(item => item.PrivacyUrl).HasMaxLength(300);
        entity.Property(item => item.TermsUrl).HasMaxLength(300);
        entity.Property(item => item.LinkedInUrl).HasMaxLength(300);
        entity.Property(item => item.TwitterUrl).HasMaxLength(300);
        entity.Property(item => item.InstagramUrl).HasMaxLength(300);
        entity.Property(item => item.FacebookUrl).HasMaxLength(300);
        entity.Property(item => item.NoticeSummer).HasMaxLength(300);
        entity.Property(item => item.NoticeSupport).HasMaxLength(300);
    }
}
