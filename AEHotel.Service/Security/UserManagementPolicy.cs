using AEHotel.Domain.Entities;
using AEHotel.Service.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace AEHotel.Service.Security;

// Call inside the same serializable transaction as the Identity writes.
public sealed class UserManagementPolicy(UserManager<AppUser> users)
{
    public async Task EnsureCanManageAsync(string actorId, IList<string> targetRoles, string? nextRole = null)
    {
        var actor = await users.FindByIdAsync(actorId);
        if (actor is null || actor.IsRestricted)
        {
            throw new ForbiddenException("An active administrator account is required.");
        }

        // Re-read Identity roles: an old JWT must not retain revoked privileges.
        var actorRoles = await users.GetRolesAsync(actor);
        var isSuperAdmin = actorRoles.Contains(AppRoles.SuperAdmin);
        if (!isSuperAdmin && !actorRoles.Contains(AppRoles.Admin))
        {
            throw new ForbiddenException("Only administrators can manage users.");
        }

        if (!isSuperAdmin && (targetRoles.Contains(AppRoles.SuperAdmin) || nextRole == AppRoles.SuperAdmin))
        {
            throw new ForbiddenException("Only SuperAdmin can manage SuperAdmin accounts or assign that role.");
        }
    }

    public async Task EnsureAnotherSuperAdminAsync(AppUser target, IList<string> currentRoles, string? nextRole, bool nextActive)
    {
        if (!currentRoles.Contains(AppRoles.SuperAdmin) || (nextRole == AppRoles.SuperAdmin && nextActive))
        {
            return;
        }

        var superAdmins = await users.GetUsersInRoleAsync(AppRoles.SuperAdmin);
        if (!superAdmins.Any(user => user.Id != target.Id && !user.IsRestricted))
        {
            throw new ForbiddenException("At least one active SuperAdmin must remain.");
        }
    }
}
