using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> entity)
    {
        entity.Property(booking => booking.Code).HasMaxLength(20).IsRequired();
        entity.Property(booking => booking.BookingType).HasMaxLength(20).IsRequired();
        entity.Property(booking => booking.Status).HasMaxLength(30).IsRequired();
        entity.Property(booking => booking.GuestName).HasMaxLength(150).IsRequired();
        entity.Property(booking => booking.GuestEmail).HasMaxLength(200).IsRequired();
        entity.Property(booking => booking.GuestPhone).HasMaxLength(40);
        entity.Property(booking => booking.ItemName).HasMaxLength(200);
        entity.Property(booking => booking.Location).HasMaxLength(200);
        entity.Property(booking => booking.SpecialRequests).HasMaxLength(1000);
        entity.Property(booking => booking.PricePerNight).HasPrecision(18, 2);
        entity.Property(booking => booking.TotalPrice).HasPrecision(18, 2);
        entity.HasIndex(booking => booking.Code).IsUnique();

        entity.HasOne(booking => booking.Hotel)
            .WithMany()
            .HasForeignKey(booking => booking.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(booking => booking.Room)
            .WithMany()
            .HasForeignKey(booking => booking.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(booking => booking.Flight)
            .WithMany()
            .HasForeignKey(booking => booking.FlightId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(booking => booking.Tour)
            .WithMany()
            .HasForeignKey(booking => booking.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(booking => booking.User)
            .WithMany(user => user.Bookings)
            .HasForeignKey(booking => booking.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
