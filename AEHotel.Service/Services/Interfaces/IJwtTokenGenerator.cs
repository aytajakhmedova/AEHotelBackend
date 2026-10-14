using AEHotel.Domain.Entities;

namespace AEHotel.Service.Services.Interfaces;

public interface IJwtTokenGenerator
{
    string CreateToken(AppUser user, IList<string> roles, out DateTime expiresAt);
}
