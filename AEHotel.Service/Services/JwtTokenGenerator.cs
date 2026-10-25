using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AEHotel.Domain.Entities;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AEHotel.Service.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string CreateToken(AppUser user, IList<string> roles, out DateTime expiresAt)
    {
        var minutes = _options.ExpiryMinutes > 0 ? _options.ExpiryMinutes : 60;
        expiresAt = DateTime.UtcNow.AddMinutes(minutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(user.Name) ? user.Email ?? user.Id : user.Name)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (user.AssignedHotelId is > 0)
        {
            claims.Add(new Claim("assignedHotelId", user.AssignedHotelId.Value.ToString()));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
