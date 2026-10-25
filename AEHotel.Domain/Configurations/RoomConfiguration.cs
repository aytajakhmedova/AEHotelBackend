using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> entity)
    {
        entity.Property(room => room.Name).HasMaxLength(200).IsRequired();
        entity.Property(room => room.Description).HasMaxLength(2000);
        entity.Property(room => room.BedType).HasMaxLength(150);
        entity.Property(room => room.Size).HasMaxLength(50);
        entity.Property(room => room.PricePerNight).HasPrecision(18, 2);
        entity.Property(room => room.HousekeepingStatus).HasMaxLength(20);
        entity.Property(room => room.ImageFile).HasMaxLength(200);
        entity.Property(room => room.AmenitiesJson).HasMaxLength(2000);

        entity.HasOne(room => room.Hotel)
            .WithMany(hotel => hotel.Rooms)
            .HasForeignKey(room => room.HotelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
