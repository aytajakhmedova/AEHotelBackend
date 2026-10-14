using AEHotel.Domain.Entities;
using AEHotel.Service.DTOs.Auth;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AEHotel.Service.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _users;
    private readonly IJwtTokenGenerator _tokens;

    public AuthService(UserManager<AppUser> users, IJwtTokenGenerator tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = (request.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new ValidationException("A valid email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("Password is required.");
        }

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Name is required.");
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            Name = name,
            Surname = string.IsNullOrWhiteSpace(request.Surname) ? name : request.Surname.Trim(),
            PhoneNumber = request.Phone?.Trim(),
            CreatedAt = DateTime.UtcNow,
            BirthDay = DateTime.UtcNow.Date
        };

        var created = await _users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            throw new ValidationException(string.Join(" ", created.Errors.Select(error => error.Description)));
        }

        await _users.AddToRoleAsync(user, AppRoles.User);
        return await IssueTokenAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = (request.Email ?? string.Empty).Trim();
        var user = await _users.FindByEmailAsync(email);
        if (user is null || !await _users.CheckPasswordAsync(user, request.Password ?? string.Empty))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (user.IsRestricted)
        {
            throw new UnauthorizedException("This account is restricted.");
        }

        return await IssueTokenAsync(user);
    }

    private async Task<AuthResponse> IssueTokenAsync(AppUser user)
    {
        var roles = await _users.GetRolesAsync(user);
        var token = _tokens.CreateToken(user, roles, out var expiresAt);
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Email = user.Email ?? string.Empty,
            Name = string.IsNullOrWhiteSpace(user.Name) ? user.Email ?? string.Empty : user.Name,
            Roles = roles.ToList()
        };
    }
}
