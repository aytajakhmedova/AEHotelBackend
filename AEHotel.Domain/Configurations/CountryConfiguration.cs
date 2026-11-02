using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> entity)
    {
        entity.Property(item => item.Name).HasMaxLength(100).IsRequired();
        entity.Property(item => item.IsoCode).HasMaxLength(2).IsRequired();
        entity.HasIndex(item => item.Name).IsUnique();
    }
}

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> entity)
    {
        entity.Property(item => item.Name).HasMaxLength(100).IsRequired();
        entity.HasOne(item => item.Country)
            .WithMany(item => item.Cities)
            .HasForeignKey(item => item.CountryId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(item => new { item.CountryId, item.Name }).IsUnique();
    }
}

public class TouristPlaceConfiguration : IEntityTypeConfiguration<TouristPlace>
{
    public void Configure(EntityTypeBuilder<TouristPlace> entity)
    {
        entity.Property(item => item.Name).HasMaxLength(150).IsRequired();
        entity.HasOne(item => item.City)
            .WithMany(item => item.TouristPlaces)
            .HasForeignKey(item => item.CityId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(item => new { item.CityId, item.Name }).IsUnique();
    }
}
