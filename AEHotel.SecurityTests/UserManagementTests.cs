using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using AEHotel.API.Security;
using AEHotel.Domain.Entities;
using AEHotel.Service.DTOs.Admin;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AEHotel.SecurityTests;

public class UserManagementTests
{
    private static object Staff(string role = AppRoles.SuperAdmin, bool active = true, string? password = null) =>
        new { Name = "Edited", Email = "edited@example.test", Role = role, IsActive = active, Password = password };

    [Theory]
    [InlineData("edit")]
    [InlineData("password")]
    [InlineData("deactivate")]
    [InlineData("delete")]
    [InlineData("remove-role")]
    [InlineData("same-role")]
    [InlineData("staff-demote")]
    public async Task Admin_cannot_manage_SuperAdmin_even_with_multiple_roles(string operation)
    {
        await using var app = await SecurityApp.CreateAsync();
        await app.AddUser("admin", false, AppRoles.Admin);
        await app.AddUser("target", false, AppRoles.Admin, AppRoles.SuperAdmin);
        await app.AddUser("other", false, AppRoles.SuperAdmin);
        app.SignIn("admin", AppRoles.Admin);
        var before = await app.Snapshot("target");
        var response = operation switch
        {
            "delete" => await app.Client.DeleteAsync("/api/staff/target"),
            "remove-role" => await app.Client.PutAsJsonAsync("/api/users/target/role", new { Role = "Admin" }),
            "same-role" => await app.Client.PutAsJsonAsync("/api/users/target/role", new { Role = "SuperAdmin" }),
            _ => await app.Client.PutAsJsonAsync("/api/staff/target", Staff(
                operation == "staff-demote" ? "Admin" : "SuperAdmin", operation != "deactivate",
                operation == "password" ? "Changed-123!" : null))
        };
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(before, await app.Snapshot("target"));
        Assert.Contains("message", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("role", "target")]
    [InlineData("role", "admin")]
    [InlineData("staff", "target")]
    [InlineData("staff", "admin")]
    [InlineData("create", "new")]
    public async Task Admin_cannot_grant_SuperAdmin_including_self_and_create(string route, string target)
    {
        await using var app = await SecurityApp.CreateAsync();
        await app.AddUser("admin", false, AppRoles.Admin);
        await app.AddUser("target", false, AppRoles.Admin);
        app.SignIn("admin", AppRoles.Admin);
        var before = await app.Snapshot(target);
        var response = route switch
        {
            "create" => await app.Client.PostAsJsonAsync("/api/staff", Staff(" sUpErAdMiN ", password: "Safe-test123!")),
            "staff" => await app.Client.PutAsJsonAsync($"/api/staff/{target}", Staff(" sUpErAdMiN ")),
            _ => await app.Client.PutAsJsonAsync($"/api/users/{target}/role", new { Role = " sUpErAdMiN " })
        };
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(before, await app.Snapshot(target));
        await app.WithScope(async services => Assert.Null(await services.GetRequiredService<UserManager<AppUser>>()
            .FindByEmailAsync("edited@example.test")));
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.SuperAdmin)]
    public async Task Administrators_can_manage_ordinary_staff(string role)
    {
        await using var app = await SecurityApp.CreateAsync();
        await app.AddUser("actor", false, role);
        app.SignIn("actor", role);
        var created = await app.Client.PostAsJsonAsync("/api/staff", Staff("Admin", password: "Safe-test123!"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var member = (await created.Content.ReadFromJsonAsync<StaffResponse>())!;
        Assert.Equal(HttpStatusCode.OK, (await app.Client.PutAsJsonAsync($"/api/staff/{member.Id}", Staff("Admin", false))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await app.Client.PutAsJsonAsync($"/api/staff/{member.Id}", Staff("Admin"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await app.Client.DeleteAsync($"/api/staff/{member.Id}")).StatusCode);
        Assert.Equal("deleted", await app.Snapshot(member.Id));
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("deactivate")]
    [InlineData("delete")]
    [InlineData("remove-role")]
    public async Task SuperAdmin_can_manage_another_when_one_active_remains(string operation)
    {
        await using var app = await SecurityApp.CreateAsync();
        await app.AddUser("actor", false, AppRoles.SuperAdmin);
        await app.AddUser("target", false, AppRoles.SuperAdmin);
        app.SignIn("actor", AppRoles.SuperAdmin);
        var response = operation switch
        {
            "delete" => await app.Client.DeleteAsync("/api/staff/target"),
            "remove-role" => await app.Client.PutAsJsonAsync("/api/users/target/role", new { Role = "Admin" }),
            _ => await app.Client.PutAsJsonAsync("/api/staff/target", Staff(active: operation != "deactivate"))
        };
        Assert.Equal(operation == "delete" ? HttpStatusCode.NoContent : HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SuperAdmin_can_grant_SuperAdmin_and_create_another()
    {
        await using var app = await SecurityApp.CreateAsync();
        await app.AddUser("actor", false, AppRoles.SuperAdmin);
        await app.AddUser("target", false, AppRoles.Admin);
        app.SignIn("actor", AppRoles.SuperAdmin);
        Assert.Equal(HttpStatusCode.OK, (await app.Client.PutAsJsonAsync("/api/users/target/role", new { Role = "SuperAdmin" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await app.Client.PostAsJsonAsync("/api/staff", Staff(password: "Safe-test123!"))).StatusCode);
    }

    [Theory]
    [InlineData("delete")]
    [InlineData("deactivate")]
    [InlineData("remove-role")]
    [InlineData("staff-demote")]
    public async Task Last_active_SuperAdmin_is_protected_in_all_routes(string operation)
    {
        await using var app = await SecurityApp.CreateAsync();
        await app.AddUser("actor", false, AppRoles.SuperAdmin, AppRoles.Admin);
        await app.AddUser("inactive", true, AppRoles.SuperAdmin);
        app.SignIn("actor", AppRoles.SuperAdmin);
        var before = await app.Snapshot("actor");
        var response = operation switch
        {
            "delete" => await app.Client.DeleteAsync("/api/staff/actor"),
            "remove-role" => await app.Client.PutAsJsonAsync("/api/users/actor/role", new { Role = "Admin" }),
            _ => await app.Client.PutAsJsonAsync("/api/staff/actor", Staff(operation == "staff-demote" ? "Admin" : "SuperAdmin", operation != "deactivate"))
        };
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(before, await app.Snapshot("actor"));
        // Exercise the shared last-active guard independently of the self-operation guard.
        await app.WithScope(async services =>
        {
            var users = services.GetRequiredService<UserManager<AppUser>>();
            var user = (await users.FindByIdAsync("actor"))!;
            await Assert.ThrowsAsync<ForbiddenException>(() => services.GetRequiredService<UserManagementPolicy>()
                .EnsureAnotherSuperAdminAsync(user, [AppRoles.SuperAdmin], operation == "deactivate" ? "SuperAdmin" : null, false));
        });
    }

    [Theory]
    [InlineData(AppRoles.Admin, false)]
    [InlineData(AppRoles.HotelManager, false)]
    [InlineData(AppRoles.Receptionist, false)]
    [InlineData(AppRoles.SuperAdmin, true)]
    public async Task Stale_SuperAdmin_JWT_does_not_bypass_current_database_permissions(string currentRole, bool restricted)
    {
        await using var app = await SecurityApp.CreateAsync();
        await app.AddUser("actor", restricted, currentRole);
        await app.AddUser("target", false, AppRoles.SuperAdmin);
        app.SignIn("actor", AppRoles.SuperAdmin);
        Assert.Equal(restricted ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, (await app.Client.DeleteAsync("/api/staff/target")).StatusCode);
        Assert.Equal(restricted ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, (await app.Client.PutAsJsonAsync("/api/users/target/role", new { Role = "Admin" })).StatusCode);
    }

    [Theory]
    [InlineData(AppRoles.HotelManager)]
    [InlineData(AppRoles.Receptionist)]
    [InlineData(AppRoles.Customer)]
    public async Task Other_roles_cannot_access_global_user_management(string role)
    {
        await using var app = await SecurityApp.CreateAsync();
        await app.AddUser("actor", false, role);
        await app.AddUser("target", false, AppRoles.Admin);
        app.SignIn("actor", role);
        Assert.Equal(HttpStatusCode.Forbidden, (await app.Client.GetAsync("/api/staff")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await app.Client.PutAsJsonAsync("/api/staff/target", Staff("Admin"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await app.Client.DeleteAsync("/api/staff/target")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await app.Client.PutAsJsonAsync("/api/users/actor/role", new { Role = "Admin" })).StatusCode);
    }

    [Fact]
    public async Task Admin_can_list_SuperAdmin_and_manage_customer_roles()
    {
        await using var app = await SecurityApp.CreateAsync();
        await app.AddUser("actor", false, AppRoles.Admin);
        await app.AddUser("target", false, AppRoles.Admin, AppRoles.SuperAdmin);
        await app.AddUser("customer", false, AppRoles.Customer);
        app.SignIn("actor", AppRoles.Admin);
        var staff = await app.Client.GetFromJsonAsync<List<StaffResponse>>("/api/staff");
        Assert.Equal("SuperAdmin", staff!.Single(user => user.Id == "target").Role);
        Assert.Equal(HttpStatusCode.OK, (await app.Client.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await app.Client.PutAsJsonAsync("/api/users/customer/role", new { Role = "Member" })).StatusCode);
    }

    [Theory]
    [InlineData(AppRoles.HotelManager)]
    [InlineData(AppRoles.Receptionist)]
    public void Existing_hotel_scope_is_preserved(string role)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role), new Claim("assignedHotelId", "7")], "test"));
        principal.EnsureHotelScope(7);
        Assert.Throws<ForbiddenException>(() => principal.EnsureHotelScope(8));
        Assert.Throws<ForbiddenException>(() => principal.EnsureHotelScope(null));
    }

    [Fact]
    public async Task Failed_password_update_rolls_back_profile_status_and_roles()
    {
        await using var app = await SecurityApp.CreateAsync();
        await app.AddUser("actor", false, AppRoles.SuperAdmin);
        await app.AddUser("target", false, AppRoles.SuperAdmin);
        app.SignIn("actor", AppRoles.SuperAdmin);
        var before = await app.Snapshot("target");
        var response = await app.Client.PutAsJsonAsync("/api/staff/target", Staff("Admin", false, "x"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await app.Snapshot("target"));
    }
}


