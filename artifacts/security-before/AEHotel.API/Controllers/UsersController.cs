using AEHotel.Domain.Entities;
using AEHotel.Service.DTOs.Users;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.API.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Staff)]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private static readonly string[] AssignableRoles =
    [
        AppRoles.SuperAdmin,
        AppRoles.Admin,
        AppRoles.HotelManager,
        AppRoles.Receptionist,
        AppRoles.Customer,
        AppRoles.Member,
        AppRoles.User
    ];

    private readonly UserManager<AppUser> _users;

    public UsersController(UserManager<AppUser> users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserAdminResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _users.Users.AsNoTracking().OrderByDescending(user => user.CreatedAt).ToListAsync(cancellationToken);
        var result = new List<UserAdminResponse>(users.Count);
        foreach (var user in users)
        {
            result.Add(new UserAdminResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Name = string.IsNullOrWhiteSpace(user.Name) ? user.Email ?? user.Id : user.Name,
                Roles = (await _users.GetRolesAsync(user)).ToList(),
                AssignedHotelId = user.AssignedHotelId,
                CreatedAt = user.CreatedAt
            });
        }

        return Ok(result);
    }

    [HttpPut("{id}/role")]
    public async Task<ActionResult<UserAdminResponse>> UpdateRole(string id, UserRoleRequest request, CancellationToken cancellationToken)
    {
        var role = (request.Role ?? string.Empty).Trim();
        if (!AssignableRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException("Unknown role.");
        }

        var normalizedRole = AssignableRoles.First(item => item.Equals(role, StringComparison.OrdinalIgnoreCase));

        // Only SuperAdmin can assign SuperAdmin role
        if (normalizedRole == AppRoles.SuperAdmin && !User.IsInRole(AppRoles.SuperAdmin))
        {
            throw new ValidationException("Only SuperAdmin can assign the SuperAdmin role.");
        }

        var user = await _users.FindByIdAsync(id)
            ?? throw new NotFoundException("User was not found.");

        var current = await _users.GetRolesAsync(user);

        // Only SuperAdmin can remove SuperAdmin role from a user
        if (current.Contains(AppRoles.SuperAdmin) && normalizedRole != AppRoles.SuperAdmin && !User.IsInRole(AppRoles.SuperAdmin))
        {
            throw new ValidationException("Only SuperAdmin can change the role of another SuperAdmin.");
        }

        if (current.Count > 0)
        {
            var removeResult = await _users.RemoveFromRolesAsync(user, current);
            if (!removeResult.Succeeded)
            {
                throw new ValidationException($"Failed to remove existing roles: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}");
            }
        }

        var addResult = await _users.AddToRoleAsync(user, normalizedRole);
        if (!addResult.Succeeded)
        {
            throw new ValidationException($"Failed to assign new role: {string.Join(", ", addResult.Errors.Select(e => e.Description))}");
        }

        user.AssignedHotelId = normalizedRole is AppRoles.HotelManager or AppRoles.Receptionist
            ? request.AssignedHotelId
            : null;

        var updateResult = await _users.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new ValidationException($"Failed to update user: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
        }

        return Ok(new UserAdminResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Name = string.IsNullOrWhiteSpace(user.Name) ? user.Email ?? user.Id : user.Name,
            Roles = (await _users.GetRolesAsync(user)).ToList(),
            AssignedHotelId = user.AssignedHotelId,
            CreatedAt = user.CreatedAt
        });
    }
}
