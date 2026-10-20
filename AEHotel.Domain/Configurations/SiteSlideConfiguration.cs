using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class SiteSlideConfiguration : IEntityTypeConfiguration<SiteSlide>
{
    public void Configure(EntityTypeBuilder<SiteSlide> entity)
    {
        entity.Property(item => item.ImageUrl).HasMaxLength(400).IsRequired();
        entity.Property(item => item.Title).HasMaxLength(200);
        entity.Property(item => item.Description).HasMaxLength(1000);
    }
}
