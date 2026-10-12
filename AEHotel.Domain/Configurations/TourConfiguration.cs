using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class TourConfiguration : IEntityTypeConfiguration<Tour>
{
    public void Configure(EntityTypeBuilder<Tour> entity)
    {
        entity.Property(tour => tour.Name).HasMaxLength(200).IsRequired();
        entity.Property(tour => tour.Destination).HasMaxLength(200);
        entity.Property(tour => tour.Duration).HasMaxLength(80);
        entity.Property(tour => tour.ImageFile).HasMaxLength(200);
        entity.Property(tour => tour.Description).HasMaxLength(4000);
        entity.Property(tour => tour.Category).HasMaxLength(100);
        entity.Property(tour => tour.Difficulty).HasMaxLength(50);
        entity.Property(tour => tour.Price).HasPrecision(18, 2);
        entity.Property(tour => tour.Rating).HasPrecision(3, 1);
    }
}
