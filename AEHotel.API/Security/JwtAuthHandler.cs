using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AEHotel.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Text;
using System.Text.Encodings.Web;
using AEHotel.Service.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AEHotel.API.Security;

public class JwtAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Bearer";

    private readonly JwtOptions _jwt;
    private readonly UserManager<AppUser> _users;

    public JwtAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<JwtOptions> jwt,
        UserManager<AppUser> users)
        : base(options, logger, encoder)
    {
        _jwt = jwt.Value;
        _users = users;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var headerValues))
        {
            return AuthenticateResult.NoResult();
        }

        var header = headerValues.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = header["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.Fail("Missing token.");
        }

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = _jwt.Issuer,
                ValidAudience = _jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
                ClockSkew = TimeSpan.FromMinutes(1),
                RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                NameClaimType = System.Security.Claims.ClaimTypes.Name
            }, out _);

            // A signed token establishes identity. Current database state establishes permissions.
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = string.IsNullOrWhiteSpace(userId) ? null : await _users.FindByIdAsync(userId);
            if (user is null || user.IsRestricted)
            {
                return AuthenticateResult.Fail("An active user account is required.");
            }

            var roles = await _users.GetRolesAsync(user);
            var identity = (ClaimsIdentity)principal.Identity!;
            foreach (var claim in identity.FindAll(ClaimTypes.Role).Concat(identity.FindAll("assignedHotelId")).ToList())
            {
                identity.RemoveClaim(claim);
            }
            foreach (var role in roles)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }

            var scoped = !roles.Contains(AppRoles.Admin) && !roles.Contains(AppRoles.SuperAdmin)
                && (roles.Contains(AppRoles.HotelManager) || roles.Contains(AppRoles.Receptionist));
            if (scoped && user.AssignedHotelId is > 0)
            {
                identity.AddClaim(new Claim("assignedHotelId", user.AssignedHotelId.Value.ToString()));
            }

            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }
        catch (Exception exception)
        {
            return AuthenticateResult.Fail(exception.Message);
        }
    }
}

