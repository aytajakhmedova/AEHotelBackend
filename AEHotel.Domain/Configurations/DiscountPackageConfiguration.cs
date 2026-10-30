using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class DiscountPackageConfiguration : IEntityTypeConfiguration<DiscountPackage>
{
    public void Configure(EntityTypeBuilder<DiscountPackage> entity)
    {
        entity.Property(item => item.Name).HasMaxLength(120).IsRequired();
        entity.Property(item => item.Combo).HasMaxLength(40).IsRequired();
        entity.Property(item => item.Code).HasMaxLength(40);
        entity.Property(item => item.Percent).HasPrecision(5, 2);
        entity.Property(item => item.Amount).HasPrecision(18, 2);
        entity.HasIndex(item => item.Combo);
    }
}
