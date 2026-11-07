using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class FlightCabinConfiguration : IEntityTypeConfiguration<FlightCabin>
{
    public void Configure(EntityTypeBuilder<FlightCabin> entity)
    {
        entity.Property(cabin => cabin.CabinClass).HasMaxLength(30).IsRequired();
        entity.Property(cabin => cabin.Price).HasPrecision(18, 2);
        // Concurrency token: two guests cannot buy the last seat of a class at the same time.
        entity.Property(cabin => cabin.RowVersion).IsRowVersion();
        entity.HasOne(cabin => cabin.Flight).WithMany(flight => flight.Cabins)
            .HasForeignKey(cabin => cabin.FlightId).OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(cabin => new { cabin.FlightId, cabin.CabinClass }).IsUnique();
    }
}
