using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class SiteMenuItemConfiguration : IEntityTypeConfiguration<SiteMenuItem>
{
    public void Configure(EntityTypeBuilder<SiteMenuItem> entity)
    {
        entity.Property(item => item.Label).HasMaxLength(80).IsRequired();
        entity.Property(item => item.Url).HasMaxLength(300).IsRequired();
        entity.Property(item => item.Placement).HasMaxLength(40).IsRequired();
        entity.Property(item => item.IconKey).HasMaxLength(40);
        entity.HasIndex(item => new { item.Placement, item.SortOrder });
    }
}
