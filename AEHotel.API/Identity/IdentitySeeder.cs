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

        await EnsureUserAsync(
            users,
            configuration["SeedAdmin:Email"],
            configuration["SeedAdmin:Password"],
            configuration["SeedAdmin:Name"] ?? "Admin",
            "User",
            AppRoles.Admin);

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
                account.AssignedHotelId);
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<AppUser> users,
        string? email,
        string? password,
        string? name,
        string? surname,
        string? role,
        int? assignedHotelId = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
        {
            return;
        }

        var user = await users.FindByEmailAsync(email);
        if (user is null)
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
        }

        if (!await users.IsInRoleAsync(user, role))
        {
            await users.AddToRoleAsync(user, role);
        }

        if (assignedHotelId.HasValue && user.AssignedHotelId != assignedHotelId)
        {
            user.AssignedHotelId = assignedHotelId;
            await users.UpdateAsync(user);
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
