using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class SiteAdvantageConfiguration : IEntityTypeConfiguration<SiteAdvantage>
{
    public void Configure(EntityTypeBuilder<SiteAdvantage> entity)
    {
        entity.Property(item => item.IconKey).HasMaxLength(40);
        entity.Property(item => item.Color).HasMaxLength(20);
        entity.Property(item => item.Title).HasMaxLength(120).IsRequired();
        entity.Property(item => item.Description).HasMaxLength(1000);
    }
}
