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
    public DbSet<Airline> Airlines { get; set; }
    public DbSet<FavoriteHotel> FavoriteHotels { get; set; }
    public DbSet<FlightCabin> FlightCabins { get; set; }
    public DbSet<TourCategory> TourCategories { get; set; }
    public DbSet<Tour> Tours { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<SiteSettings> SiteSettings { get; set; }
    public DbSet<SiteMenuItem> SiteMenuItems { get; set; }
    public DbSet<SiteSlide> SiteSlides { get; set; }
    public DbSet<SiteAdvantage> SiteAdvantages { get; set; }
    public DbSet<SeasonalPrice> SeasonalPrices { get; set; }
    public DbSet<DiscountPackage> DiscountPackages { get; set; }
    public DbSet<StayReview> StayReviews { get; set; }
    public DbSet<Inquiry> Inquiries { get; set; }
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
        
        builder.Entity<Tour>()
            .HasOne(tour => tour.Hotel)
            .WithMany()
            .HasForeignKey(tour => tour.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        var tourCategories = builder.Entity<TourCategory>();
        tourCategories.Property(item => item.Name).HasMaxLength(100).IsRequired();
        tourCategories.HasIndex(item => item.Name).IsUnique();

        var airlines = builder.Entity<Airline>();
        airlines.Property(item => item.Name).HasMaxLength(100).IsRequired();
        airlines.Property(item => item.Code).HasMaxLength(3);
        airlines.HasIndex(item => item.Name).IsUnique();

        var favorites = builder.Entity<FavoriteHotel>();
        favorites.Property(item => item.UserId).HasMaxLength(450).IsRequired();
        favorites.HasIndex(item => new { item.UserId, item.HotelId }).IsUnique();
        favorites.HasOne(item => item.Hotel).WithMany().HasForeignKey(item => item.HotelId).OnDelete(DeleteBehavior.Cascade);

        // Concurrency token for Flight to prevent overbooking
        builder.Entity<Flight>()
            .Property(f => f.RowVersion)
            .IsRowVersion();
    }
}
