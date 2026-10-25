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
        user.IsInRole(AppRoles.Admin)
        || user.IsInRole(AppRoles.SuperAdmin)
        || user.IsInRole(AppRoles.HotelManager)
        || user.IsInRole(AppRoles.Receptionist);

    public static bool TryGetHotelScope(this ClaimsPrincipal user, out int hotelId)
    {
        hotelId = 0;
        if (user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.SuperAdmin))
        {
            return false;
        }

        if (!user.IsInRole(AppRoles.HotelManager) && !user.IsInRole(AppRoles.Receptionist))
        {
            return false;
        }

        var raw = user.FindFirstValue("assignedHotelId");
        if (int.TryParse(raw, out var id) && id > 0)
        {
            hotelId = id;
            return true;
        }

        hotelId = 0;
        return true;
    }

    public static void EnsureHotelScope(this ClaimsPrincipal user, int? hotelId)
    {
        if (!user.TryGetHotelScope(out var assigned))
        {
            return;
        }

        if (assigned == 0 || hotelId != assigned)
        {
            throw new ForbiddenException("Bu otel sizin tapşırığınıza daxil deyil.");
        }
    }
}
