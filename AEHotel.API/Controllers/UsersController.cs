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

        var user = await _users.FindByIdAsync(id)
            ?? throw new NotFoundException("User was not found.");

        var current = await _users.GetRolesAsync(user);
        if (current.Count > 0)
        {
            await _users.RemoveFromRolesAsync(user, current);
        }

        await _users.AddToRoleAsync(user, AssignableRoles.First(item => item.Equals(role, StringComparison.OrdinalIgnoreCase)));
        user.AssignedHotelId = role is AppRoles.HotelManager or AppRoles.Receptionist
            ? request.AssignedHotelId
            : null;
        await _users.UpdateAsync(user);
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
