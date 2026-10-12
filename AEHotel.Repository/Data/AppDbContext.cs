using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Hotel> Hotels => Set<Hotel>();

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hotel>(entity =>
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
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.Property(room => room.Name).HasMaxLength(200).IsRequired();
            entity.Property(room => room.Description).HasMaxLength(2000);
            entity.Property(room => room.BedType).HasMaxLength(150);
            entity.Property(room => room.Size).HasMaxLength(50);
            entity.Property(room => room.PricePerNight).HasPrecision(18, 2);

            entity.HasOne(room => room.Hotel)
                .WithMany(hotel => hotel.Rooms)
                .HasForeignKey(room => room.HotelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Reservation>(entity =>
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
        });

        modelBuilder.Entity<Hotel>().HasData(new Hotel
        {
            Id = 1,
            Title = "Çendu Corinthia",
            City = "Çendu",
            Country = "Çin",
            Location = "Gui Xi Şəhər Parkının yanı",
            Address = "No. 88, Jinrong Avenue, Tianfu District",
            Stars = 5,
            Rating = 4.8m,
            ReviewCount = 1284,
            Description = "Gui Xi Şəhər Parkının yanında ucalan lüks otel.",
            PricePerNight = 420m,
            HotelType = "Lüks",
            CheckInTime = "14:00",
            CheckOutTime = "12:00"
        });

        modelBuilder.Entity<Room>().HasData(
            new Room
            {
                Id = 1,
                HotelId = 1,
                Name = "Standart Otaq",
                Description = "Rahat və müasir dizaynlı standart otaq.",
                Capacity = 2,
                BedType = "1 King yataq",
                Size = "32 m2",
                PricePerNight = 420m,
                Available = true
            },
            new Room
            {
                Id = 2,
                HotelId = 1,
                Name = "Deluxe Room",
                Description = "Şəhər mənzərəli geniş otaq.",
                Capacity = 3,
                BedType = "1 King yataq + 1 Sofa yataq",
                Size = "45 m2",
                PricePerNight = 580m,
                Available = true
            });
    }
}
