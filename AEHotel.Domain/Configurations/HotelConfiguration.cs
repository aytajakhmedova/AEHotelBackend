using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class HotelConfiguration : IEntityTypeConfiguration<Hotel>
{
    public void Configure(EntityTypeBuilder<Hotel> entity)
    {
        entity.Property(hotel => hotel.Title).HasMaxLength(200).IsRequired();
        entity.Property(hotel => hotel.City).HasMaxLength(100).IsRequired();
        entity.Property(hotel => hotel.Country).HasMaxLength(100).IsRequired();
        entity.Property(hotel => hotel.Location).HasMaxLength(300);
        entity.Property(hotel => hotel.Address).HasMaxLength(300);
        entity.Property(hotel => hotel.Description).HasMaxLength(4000);
        entity.Property(hotel => hotel.HotelType).HasMaxLength(100);
        entity.Property(hotel => hotel.CheckInTime).HasMaxLength(10);
        entity.Property(hotel => hotel.CheckOutTime).HasMaxLength(10);
        entity.Property(hotel => hotel.PricePerNight).HasPrecision(18, 2);
        entity.Property(hotel => hotel.Rating).HasPrecision(3, 1);
        entity.Property(hotel => hotel.ImageFile).HasMaxLength(200);
        entity.Property(hotel => hotel.AmenitiesJson).HasMaxLength(2000);
    }
}
