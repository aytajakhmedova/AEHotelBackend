using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using AEHotel.Service.DTOs.Users;
using AEHotel.Service.Security;
using AEHotel.Service.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AEHotel.SecurityTests;

public class HotelAssignmentTests
{
    private static async Task SeedHotels(SecurityApp app)
    {
        await app.WithScope(async services =>
        {
            var db = services.GetRequiredService<AppDbContext>();
            db.Hotels.AddRange(new Hotel { Id = 1, Title = "First" },
                new Hotel { Id = 2, Title = "Second" }, new Hotel { Id = 3, Title = "Deleted", IsDeleted = true });
            await db.SaveChangesAsync();
        });
    }

    private static Task AssignLegacyHotel(SecurityApp app, string id, int hotelId) =>
        app.WithScope(async services =>
        {
            var users = services.GetRequiredService<UserManager<AppUser>>();
            var user = (await users.FindByIdAsync(id))!;
            user.AssignedHotelId = hotelId;
            Assert.True((await users.UpdateAsync(user)).Succeeded);
        });

    private static async Task<string> Token(SecurityApp app, string id)
    {
        var token = "";
        await app.WithScope(async services =>
        {
            var users = services.GetRequiredService<UserManager<AppUser>>();
            var user = (await users.FindByIdAsync(id))!;
            token = new JwtTokenGenerator(services.GetRequiredService<IOptions<JwtOptions>>())
                .CreateToken(user, await users.GetRolesAsync(user), out _);
        });
        return token;
    }

    [Theory]
    [InlineData(AppRoles.Customer)]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.SuperAdmin)]
    [InlineData(AppRoles.Member)]
    public async Task Legacy_assignment_is_hidden_for_non_scoped_roles(string role)
    {
        await using var app = await SecurityApp.CreateAsync();
        await SeedHotels(app);
        await app.AddUser("actor", false, AppRoles.Admin);
        await app.AddUser("target", false, role);
        await AssignLegacyHotel(app, "target", 1);
        app.SignIn("actor", AppRoles.Admin);
        var users = await app.Client.GetFromJsonAsync<List<UserAdminResponse>>("/api/users");
        Assert.Null(users!.Single(user => user.Id == "target").AssignedHotelId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(999)]
    public async Task Invalid_assignment_is_rejected_atomically(int? hotelId)
    {
        await using var app = await SecurityApp.CreateAsync();
        await SeedHotels(app);
        await app.AddUser("actor", false, AppRoles.Admin);
        await app.AddUser("target", false, AppRoles.Customer);
        app.SignIn("actor", AppRoles.Admin);
        var before = await app.Snapshot("target");
        var result = await app.Client.PutAsJsonAsync("/api/users/target/role",
            new { Role = AppRoles.HotelManager, AssignedHotelId = hotelId });
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal(before, await app.Snapshot("target"));
    }

    [Theory]
    [InlineData(AppRoles.HotelManager)]
    [InlineData(AppRoles.Receptionist)]
    public async Task Scoped_role_requires_and_persists_hotel(string role)
    {
        await using var app = await SecurityApp.CreateAsync();
        await SeedHotels(app);
        await app.AddUser("actor", false, AppRoles.Admin);
        await app.AddUser("target", false, AppRoles.Customer);
        app.SignIn("actor", AppRoles.Admin);
        Assert.Equal(HttpStatusCode.OK, (await app.Client.PutAsJsonAsync("/api/users/target/role",
            new { Role = role, AssignedHotelId = 2 })).StatusCode);
        var users = await app.Client.GetFromJsonAsync<List<UserAdminResponse>>("/api/users");
        Assert.Equal(2, users!.Single(user => user.Id == "target").AssignedHotelId);
    }

    [Theory]
    [InlineData(AppRoles.Customer)]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.SuperAdmin)]
    public async Task Leaving_hotel_role_clears_assignment_and_preserves_booking_history(string nextRole)
    {
        await using var app = await SecurityApp.CreateAsync();
        await SeedHotels(app);
        await app.AddUser("actor", false, AppRoles.SuperAdmin);
        await app.AddUser("target", false, AppRoles.HotelManager);
        await AssignLegacyHotel(app, "target", 1);
        await app.WithScope(async services =>
        {
            var db = services.GetRequiredService<AppDbContext>();
            db.Rooms.Add(new Room { Id = 1, HotelId = 1 });
            db.Bookings.Add(new Booking { Id = 1, UserId = "target", HotelId = 2, Code = "saved-booking", TotalPrice = 175 });
            db.Reservations.Add(new Reservation { Id = 1, UserId = "target", HotelId = 1, RoomId = 1, Code = "saved-reservation", TotalPrice = 250 });
            await db.SaveChangesAsync();
        });
        var oldToken = await Token(app, "target");
        app.SignIn("actor", AppRoles.SuperAdmin);
        var result = await app.Client.PutAsJsonAsync("/api/users/target/role",
            new { Role = nextRole, AssignedHotelId = 1 });
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null((await result.Content.ReadFromJsonAsync<UserAdminResponse>())!.AssignedHotelId);
        await app.WithScope(async services =>
        {
            var db = services.GetRequiredService<AppDbContext>();
            Assert.Null((await db.Users.SingleAsync(user => user.Id == "target")).AssignedHotelId);
            var booking = await db.Bookings.SingleAsync();
            Assert.Equal("target", booking.UserId);
            Assert.Equal(2, booking.HotelId);
            Assert.Equal(175, booking.TotalPrice);
            var reservation = await db.Reservations.SingleAsync();
            Assert.Equal("saved-reservation", reservation.Code);
            Assert.Equal(250, reservation.TotalPrice);
        });
        app.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldToken);
        if (nextRole == AppRoles.Customer)
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await app.Client.PutAsJsonAsync("/api/hotels/1", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await app.Client.GetAsync("/api/bookings")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await app.Client.GetAsync("/security-test/hotel/1")).StatusCode);
        }
        else
        {
            Assert.Equal(HttpStatusCode.OK, (await app.Client.GetAsync("/security-test/hotel/2")).StatusCode);
        }
    }

    [Fact]
    public async Task Existing_token_uses_current_hotel_assignment()
    {
        await using var app = await SecurityApp.CreateAsync();
        await SeedHotels(app);
        await app.AddUser("actor", false, AppRoles.Admin);
        await app.AddUser("target", false, AppRoles.HotelManager);
        await AssignLegacyHotel(app, "target", 1);
        var token = await Token(app, "target");
        app.SignIn("actor", AppRoles.Admin);
        Assert.Equal(HttpStatusCode.OK, (await app.Client.PutAsJsonAsync("/api/users/target/role",
            new { Role = AppRoles.HotelManager, AssignedHotelId = 2 })).StatusCode);
        app.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.Forbidden, (await app.Client.GetAsync("/security-test/hotel/1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await app.Client.GetAsync("/security-test/hotel/2")).StatusCode);
    }

    [Fact]
    public async Task Customer_with_legacy_assignment_has_no_hotel_management_access()
    {
        await using var app = await SecurityApp.CreateAsync();
        await SeedHotels(app);
        await app.AddUser("target", false, AppRoles.Customer);
        await AssignLegacyHotel(app, "target", 1);
        app.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await Token(app, "target"));
        Assert.Equal(HttpStatusCode.Forbidden, (await app.Client.GetAsync("/security-test/hotel/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await app.Client.PutAsJsonAsync("/api/hotels/1", new { })).StatusCode);
    }
}

