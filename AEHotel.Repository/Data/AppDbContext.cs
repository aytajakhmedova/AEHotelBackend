using AEHotel.Domain.Configurations;
using AEHotel.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public DbSet<Hotel> Hotels { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Flight> Flights { get; set; }
    public DbSet<Tour> Tours { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<SiteSettings> SiteSettings { get; set; }
    public DbSet<SiteMenuItem> SiteMenuItems { get; set; }
    public DbSet<SiteSlide> SiteSlides { get; set; }
    public DbSet<SiteAdvantage> SiteAdvantages { get; set; }
    public DbSet<DiscountPackage> DiscountPackages { get; set; }
    public DbSet<StayReview> StayReviews { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<TouristPlace> TouristPlaces { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        var stays = builder.Entity<BookingRoom>();
        stays.ToTable("BookingRooms");
        stays.HasKey(stay => new { stay.BookingId, stay.RoomId });
        stays.HasOne<Booking>().WithMany(booking => booking.Rooms).HasForeignKey(stay => stay.BookingId);
        stays.HasOne<Room>().WithMany().HasForeignKey(stay => stay.RoomId).OnDelete(DeleteBehavior.Restrict);
        stays.Property(stay => stay.RoomName).HasMaxLength(200);
        stays.Property(stay => stay.PricePerNight).HasPrecision(18, 2);
        builder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
        builder.Entity<StayReview>()
            .HasIndex(item => item.BookingId)
            .IsUnique()
            .HasFilter("[BookingId] IS NOT NULL");
        builder.Entity<DiscountPackage>()
            .HasIndex(item => item.Code)
            .IsUnique()
            .HasFilter("[Code] IS NOT NULL AND [Code] <> ''");
        
        // Concurrency token for Flight to prevent overbooking
        builder.Entity<Flight>()
            .Property(f => f.RowVersion)
            .IsRowVersion();
    }
}
