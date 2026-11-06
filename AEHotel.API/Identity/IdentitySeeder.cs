using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore;
using AEHotel.Domain.Entities;
using AEHotel.Service.Security;
using Microsoft.AspNetCore.Identity;

namespace AEHotel.API.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var roleName in new[]
                 {
                     AppRoles.SuperAdmin, AppRoles.Admin, AppRoles.HotelManager, AppRoles.Receptionist,
                     AppRoles.Customer, AppRoles.Member, AppRoles.User
                 })
        {
            if (!await roles.RoleExistsAsync(roleName))
            {
                await roles.CreateAsync(new IdentityRole(roleName));
            }
        }

        var users = services.GetRequiredService<UserManager<AppUser>>();
        var db = services.GetRequiredService<AppDbContext>();
        var activeHotelIds = await db.Hotels.Where(hotel => !hotel.IsDeleted).OrderBy(hotel => hotel.Id).Select(hotel => hotel.Id).ToListAsync();

        await EnsureUserAsync(
            users,
            configuration["SeedAdmin:Email"],
            configuration["SeedAdmin:Password"],
            configuration["SeedAdmin:Name"] ?? "Admin",
            "User",
            AppRoles.Admin,
            activeHotelIds: activeHotelIds);

        var extras = configuration.GetSection("SeedUsers").Get<SeedAccount[]>() ?? [];
        foreach (var account in extras)
        {
            await EnsureUserAsync(
                users,
                account.Email,
                account.Password,
                account.Name,
                account.Surname,
                account.Role,
                account.AssignedHotelId,
                activeHotelIds);
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<AppUser> users,
        string? email,
        string? password,
        string? name,
        string? surname,
        string? role,
        int? assignedHotelId = null,
        IReadOnlyList<int>? activeHotelIds = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
        {
            return;
        }

        var user = await users.FindByEmailAsync(email);
        var isNewUser = user is null;
        
        if (isNewUser)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Name = string.IsNullOrWhiteSpace(name) ? "Aytac" : name.Trim(),
                Surname = string.IsNullOrWhiteSpace(surname) ? role : surname.Trim(),
                AssignedHotelId = assignedHotelId,
                CreatedAt = DateTime.UtcNow,
                BirthDay = DateTime.UtcNow.Date
            };

            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException($"User {email} could not be seeded: " +
                    string.Join(" ", created.Errors.Select(error => error.Description)));
            }

            // Only add role for new users
            var addRoleResult = await users.AddToRoleAsync(user, role);
            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException($"Role {role} could not be assigned to {email}: " +
                    string.Join(" ", addRoleResult.Errors.Select(error => error.Description)));
            }
        }
        // For existing users, do NOT modify their roles - respect admin changes

        // A seeded staff account keeps the hotel an admin gave it. The configured hotel is only applied when the
        // account has none, or its hotel no longer exists (ids change when the catalog is reseeded).
        if (assignedHotelId.HasValue && activeHotelIds is { Count: > 0 })
        {
            var currentIsValid = user.AssignedHotelId.HasValue && activeHotelIds.Contains(user.AssignedHotelId.Value);
            if (!currentIsValid)
            {
                user.AssignedHotelId = activeHotelIds.Contains(assignedHotelId.Value) ? assignedHotelId.Value : activeHotelIds[0];
                await users.UpdateAsync(user);
            }
        }
    }

    private sealed class SeedAccount
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = "Aytac";
        public string Surname { get; set; } = string.Empty;
        public string Role { get; set; } = AppRoles.User;
        public int? AssignedHotelId { get; set; }
    }
}
