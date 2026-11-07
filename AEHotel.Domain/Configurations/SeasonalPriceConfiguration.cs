using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class SeasonalPriceConfiguration : IEntityTypeConfiguration<SeasonalPrice>
{
    public void Configure(EntityTypeBuilder<SeasonalPrice> entity)
    {
        entity.Property(item => item.Country).HasMaxLength(100).IsRequired();
        entity.Property(item => item.Currency).HasMaxLength(3).IsRequired();
        entity.Property(item => item.MinPrice).HasPrecision(18, 2);
        entity.Property(item => item.MaxPrice).HasPrecision(18, 2);
        entity.HasIndex(item => new { item.Country, item.Month }).IsUnique();
    }
}
