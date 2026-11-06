using System.Text;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using AEHotel.Repository.Repositories;
using AEHotel.Service.DTOs.Bookings;
using AEHotel.Service.DTOs.Email;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Services;
using AEHotel.Service.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AEHotel.SecurityTests;

public class HotelStayTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task One_booking_and_receipt_cover_every_selected_room(int count)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var hotel = new Hotel { Title = "Test Hotel" };
        var rooms = Enumerable.Range(1, count).Select(i =>
            new Room { Hotel = hotel, Name = $"Test Room {i}", Capacity = 2, PricePerNight = i * 100 }).ToList();
        db.Rooms.AddRange(rooms);
        var user = new AppUser { UserName = "stay-test" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var hotels = new HotelRepository(db);
        var flights = new FlightRepository(db);
        var tours = new TourRepository(db);
        var mail = new CapturedMail();
        var service = new BookingService(new BookingRepository(db), hotels, new RoomRepository(db),
            new ReservationRepository(db), flights, tours,
            new TripSuggestionService(flights, tours, hotels, new LocationRepository(db)),
            new DiscountService(new DiscountPackageRepository(db)), mail, NullLogger<BookingService>.Instance);
        var arrival = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        var request = new BookingRequest
        {
            HotelId = hotel.Id, RoomIds = rooms.Select(r => r.Id).ToList(), Rooms = count,
            Adults = count, Children = count, CheckIn = arrival, CheckOut = arrival.AddDays(2),
            GuestName = "Test Guest", GuestEmail = "guest@example.test", PaymentMethod = "cash"
        };
        var quote = await service.QuoteHotelAsync(hotel.Id, request);
        Assert.Equal(rooms.Sum(r => r.PricePerNight) * 2 * 1.18m, quote.Total);

        request.ExpectedStayTotal = quote.Total - 1;
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request, user.Id));
        Assert.False(await db.Bookings.AnyAsync());
        request.ExpectedStayTotal = quote.Total;
        var booking = await service.CreateAsync(request, user.Id);
        Assert.Equal(quote.Total, booking.TotalPrice);
        Assert.Equal(quote.Rooms.ToArray(), booking.Rooms.ToArray());
        Assert.Equal(1, await db.Payments.CountAsync());
        Assert.True(booking.EmailSent);
        foreach (var room in booking.Rooms)
        {
            Assert.Equal((1, 1), (room.Adults, room.Children));
            Assert.Contains(room.RoomName, mail.Html);
            var collision = new HotelStayRequest { RoomId = room.RoomId, CheckIn = arrival, CheckOut = arrival.AddDays(1) };
            await Assert.ThrowsAsync<ValidationException>(() => service.QuoteHotelAsync(hotel.Id, collision));
        }
        Assert.Equal("application/pdf", mail.Pdf.ContentType);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(mail.Pdf.Content, 0, 5));

        // Simulate a pre-migration booking that has only Bookings.RoomId.
        var legacy = new Booking { HotelId = hotel.Id, RoomId = rooms[0].Id, UserId = user.Id, Code = "OLD-ROOM",
            CheckIn = arrival.AddDays(2), CheckOut = arrival.AddDays(3), Guests = 2, PricePerNight = 100, TotalPrice = 118 };
        db.Bookings.Add(legacy);
        await db.SaveChangesAsync();
        var old = await service.GetByIdAsync(legacy.Id, user.Id, false);
        Assert.Equal(rooms[0].Id, Assert.Single(old.Rooms).RoomId);
        await Assert.ThrowsAsync<ValidationException>(() => service.QuoteHotelAsync(hotel.Id, new HotelStayRequest {
            RoomId = rooms[0].Id, CheckIn = legacy.CheckIn, CheckOut = legacy.CheckOut }));
    }

    private sealed class CapturedMail : IEmailSender
    {
        public bool IsConfigured => true;
        public bool DeliversToInbox => true;
        public string Html { get; private set; } = string.Empty;
        public EmailAttachment Pdf { get; private set; } = new();
        public Task SendAsync(string to, string subject, string htmlBody,
            IReadOnlyList<EmailAttachment>? attachments = null, CancellationToken cancellationToken = default)
        {
            Html = htmlBody;
            Pdf = Assert.Single(attachments!);
            return Task.CompletedTask;
        }
    }
}
