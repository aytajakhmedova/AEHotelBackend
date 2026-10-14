using AEHotel.Domain.Entities;
using AEHotel.Service.Security;
using Microsoft.AspNetCore.Identity;

namespace AEHotel.API.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var roleName in new[] { AppRoles.Admin, AppRoles.User })
        {
            if (!await roles.RoleExistsAsync(roleName))
            {
                await roles.CreateAsync(new IdentityRole(roleName));
            }
        }

        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var users = services.GetRequiredService<UserManager<AppUser>>();
        var admin = await users.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Name = configuration["SeedAdmin:Name"] ?? "Admin",
                Surname = "User",
                CreatedAt = DateTime.UtcNow,
                BirthDay = DateTime.UtcNow.Date
            };

            var created = await users.CreateAsync(admin, password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException("Admin user could not be seeded: " +
                    string.Join(" ", created.Errors.Select(error => error.Description)));
            }
        }

        if (!await users.IsInRoleAsync(admin, AppRoles.Admin))
        {
            await users.AddToRoleAsync(admin, AppRoles.Admin);
        }
    }
}
