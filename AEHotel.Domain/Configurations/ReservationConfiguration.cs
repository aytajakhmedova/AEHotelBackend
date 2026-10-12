using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> entity)
    {
        entity.Property(reservation => reservation.Code).HasMaxLength(20).IsRequired();
        entity.Property(reservation => reservation.Status).HasMaxLength(30).IsRequired();
        entity.Property(reservation => reservation.GuestName).HasMaxLength(150).IsRequired();
        entity.Property(reservation => reservation.GuestEmail).HasMaxLength(200).IsRequired();
        entity.Property(reservation => reservation.PricePerNight).HasPrecision(18, 2);
        entity.Property(reservation => reservation.TotalPrice).HasPrecision(18, 2);
        entity.HasIndex(reservation => reservation.Code).IsUnique();

        entity.HasOne(reservation => reservation.Hotel)
            .WithMany(hotel => hotel.Reservations)
            .HasForeignKey(reservation => reservation.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(reservation => reservation.Room)
            .WithMany(room => room.Reservations)
            .HasForeignKey(reservation => reservation.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(reservation => reservation.User)
            .WithMany(user => user.Reservations)
            .HasForeignKey(reservation => reservation.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
