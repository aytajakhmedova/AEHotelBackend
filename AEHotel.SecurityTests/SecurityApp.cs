using System.Net.Http.Headers;
using AEHotel.API.Controllers;
using AEHotel.API.Middleware;
using AEHotel.API.Security;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using AEHotel.Service.Security;
using AEHotel.Service.Services;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace AEHotel.SecurityTests;

// Does not execute production Program.cs, migrations, seeders, or read its connection string.
internal sealed class SecurityApp : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplication _app;
    private readonly JwtOptions _jwt;
    public HttpClient Client { get; }

    private SecurityApp(SqliteConnection connection, WebApplication app, JwtOptions jwt)
    {
        _connection = connection;
        _app = app;
        _jwt = jwt;
        Client = app.GetTestClient();
    }

    public static async Task<SecurityApp> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        builder.Services.AddIdentityCore<AppUser>()
            .AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
        var jwt = new JwtOptions
        {
            Key = "Security-tests-only-key-never-used-in-production-12345",
            Issuer = "security-tests", Audience = "security-tests", ExpiryMinutes = 10
        };
        builder.Services.Configure<JwtOptions>(options =>
        {
            options.Key = jwt.Key; options.Issuer = jwt.Issuer;
            options.Audience = jwt.Audience; options.ExpiryMinutes = jwt.ExpiryMinutes;
        });
        builder.Services.AddAuthentication(JwtAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, JwtAuthHandler>(JwtAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddScoped<UserManagementPolicy>();
        builder.Services.AddScoped<IAdminService, AdminService>();
        builder.Services.AddControllers().AddApplicationPart(typeof(UsersController).Assembly);
        var app = builder.Build();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        // Exercise the production authentication and hotel-scope checks without catalog service fixtures.
        app.MapGet("/security-test/hotel/{id:int}", (int id, System.Security.Claims.ClaimsPrincipal user) =>
        {
            user.EnsureHotelScope(id);
            return Microsoft.AspNetCore.Http.Results.Ok();
        }).RequireAuthorization(policy => policy.RequireRole(AppRoles.HotelManager, AppRoles.Receptionist, AppRoles.Admin, AppRoles.SuperAdmin));
        await app.StartAsync();
        var result = new SecurityApp(connection, app, jwt);
        await result.WithScope(async services =>
        {
            await services.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
            var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role in new[] { AppRoles.SuperAdmin, AppRoles.Admin, AppRoles.HotelManager,
                         AppRoles.Receptionist, AppRoles.Customer, AppRoles.Member, AppRoles.User })
                Assert.True((await roles.CreateAsync(new IdentityRole(role))).Succeeded);
        });
        return result;
    }

    public async Task WithScope(Func<IServiceProvider, Task> action)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        await action(scope.ServiceProvider);
    }

    public Task AddUser(string id, bool restricted = false, params string[] roles) => WithScope(async services =>
    {
        var users = services.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser { Id = id, UserName = id + "@example.test", Email = id + "@example.test",
            Name = id, Surname = id, IsRestricted = restricted, BirthDay = new DateTime(1990, 1, 1) };
        Assert.True((await users.CreateAsync(user, "Safe-test123!")).Succeeded);
        Assert.True((await users.AddToRolesAsync(user, roles)).Succeeded);
    });

    public void SignIn(string id, params string[] tokenRoles)
    {
        var token = new JwtTokenGenerator(Options.Create(_jwt)).CreateToken(
            new AppUser { Id = id, Email = id + "@example.test" }, tokenRoles, out _);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<string> Snapshot(string id)
    {
        var value = "deleted";
        await WithScope(async services =>
        {
            var users = services.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByIdAsync(id);
            if (user is not null)
                value = System.Text.Json.JsonSerializer.Serialize(new { user.Name, user.Email, user.IsRestricted,
                    user.AssignedHotelId, user.PasswordHash, Roles = (await users.GetRolesAsync(user)).Order() });
        });
        return value;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

