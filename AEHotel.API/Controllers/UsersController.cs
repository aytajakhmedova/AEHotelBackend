using System.Data;
using AEHotel.API.Security;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
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
    private readonly AppDbContext _db;
    private readonly UserManagementPolicy _management;

    public UsersController(UserManager<AppUser> users, AppDbContext db, UserManagementPolicy management)
    {
        _users = users;
        _db = db;
        _management = management;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserAdminResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _users.Users.AsNoTracking().OrderByDescending(user => user.CreatedAt).ToListAsync(cancellationToken);
        var result = new List<UserAdminResponse>(users.Count);
        foreach (var user in users)
        {
            var roles = await _users.GetRolesAsync(user);
            result.Add(new UserAdminResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Name = string.IsNullOrWhiteSpace(user.Name) ? user.Email ?? user.Id : user.Name,
                Roles = roles.ToList(),
                AssignedHotelId = !roles.Contains(AppRoles.Admin) && !roles.Contains(AppRoles.SuperAdmin)
                    && (roles.Contains(AppRoles.HotelManager) || roles.Contains(AppRoles.Receptionist))
                    ? user.AssignedHotelId : null,
                CreatedAt = user.CreatedAt
            });
        }

        return Ok(result);
    }

    [HttpPut("{id}/role")]
    public async Task<ActionResult<UserAdminResponse>> UpdateRole(string id, UserRoleRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var user = await _users.FindByIdAsync(id)
            ?? throw new NotFoundException("User was not found.");
        var current = await _users.GetRolesAsync(user);
        var actorId = User.GetRequiredUserId();
        await _management.EnsureCanManageAsync(actorId, current);

        var role = (request.Role ?? string.Empty).Trim();
        if (!AssignableRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException("Unknown role.");
        }

        var normalizedRole = AssignableRoles.First(item => item.Equals(role, StringComparison.OrdinalIgnoreCase));

        await _management.EnsureCanManageAsync(actorId, current, normalizedRole);
        if (id == actorId && !current.Contains(normalizedRole))
        {
            throw new ForbiddenException("You cannot change the role of your own account.");
        }

        await _management.EnsureAnotherSuperAdminAsync(user, current, normalizedRole, !user.IsRestricted);

        var requiresHotel = normalizedRole is AppRoles.HotelManager or AppRoles.Receptionist;
        if (requiresHotel)
        {
            if (request.AssignedHotelId is not > 0)
            {
                throw new ValidationException("A hotel must be assigned to a Hotel Manager or Receptionist.");
            }

            if (!await _db.Hotels.AnyAsync(hotel => hotel.Id == request.AssignedHotelId && !hotel.IsDeleted, cancellationToken))
            {
                throw new ValidationException("The selected hotel does not exist.");
            }
        }

        if (!current.Contains(normalizedRole) || current.Count != 1)
        {
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
        }

        user.AssignedHotelId = requiresHotel
            ? request.AssignedHotelId
            : null;

        var updateResult = await _users.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new ValidationException($"Failed to update user: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
        }

        var response = new UserAdminResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Name = string.IsNullOrWhiteSpace(user.Name) ? user.Email ?? user.Id : user.Name,
            Roles = (await _users.GetRolesAsync(user)).ToList(),
            AssignedHotelId = user.AssignedHotelId,
            CreatedAt = user.CreatedAt
        };
        await transaction.CommitAsync(cancellationToken);
        return Ok(response);
    }
}

