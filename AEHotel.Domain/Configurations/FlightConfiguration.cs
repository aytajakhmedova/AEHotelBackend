using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class FlightConfiguration : IEntityTypeConfiguration<Flight>
{
    public void Configure(EntityTypeBuilder<Flight> entity)
    {
        entity.Property(flight => flight.Airline).HasMaxLength(100).IsRequired();
        entity.Property(flight => flight.FlightNumber).HasMaxLength(20).IsRequired();
        entity.Property(flight => flight.OriginCode).HasMaxLength(10);
        entity.Property(flight => flight.OriginCity).HasMaxLength(100);
        entity.Property(flight => flight.OriginCountry).HasMaxLength(100);
        entity.Property(flight => flight.DestinationCode).HasMaxLength(10);
        entity.Property(flight => flight.DestinationCity).HasMaxLength(100);
        entity.Property(flight => flight.DestinationCountry).HasMaxLength(100);
        entity.Property(flight => flight.Duration).HasMaxLength(40);
        entity.Property(flight => flight.CabinClass).HasMaxLength(30);
        entity.Property(flight => flight.Price).HasPrecision(18, 2);
    }
}
