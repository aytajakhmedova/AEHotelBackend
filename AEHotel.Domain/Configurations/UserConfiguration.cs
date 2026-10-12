using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> entity)
    {
        entity.Property(user => user.Name).HasMaxLength(100).IsRequired();
        entity.Property(user => user.Surname).HasMaxLength(100).IsRequired();
        entity.Property(user => user.FinKod).HasMaxLength(7);
        entity.HasIndex(user => user.FinKod).IsUnique();
    }
}
