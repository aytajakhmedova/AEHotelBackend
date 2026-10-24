using System.Security.Claims;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Security;

namespace AEHotel.API.Security;

public static class UserClaimsExtensions
{
    public static string GetRequiredUserId(this ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedException("Authentication is required.");
        }

        return userId;
    }

    public static bool IsAdmin(this ClaimsPrincipal user) =>
        user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.SuperAdmin);
}
