using System.Net;
using AEHotel.Domain.Entities;
using AEHotel.Service.DTOs.Auth;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AEHotel.Service.Services;

public class AuthService : IAuthService
{
    private const string ForgotPasswordMessage =
        "If an account exists for that email, a password reset link has been sent.";

    private const string InvalidResetMessage =
        "This password reset link is invalid or has expired.";

    private readonly UserManager<AppUser> _users;
    private readonly IJwtTokenGenerator _tokens;
    private readonly IEmailSender _email;
    private readonly EmailOptions _emailOptions;

    public AuthService(
        UserManager<AppUser> users,
        IJwtTokenGenerator tokens,
        IEmailSender email,
        IOptions<EmailOptions> emailOptions)
    {
        _users = users;
        _tokens = tokens;
        _email = email;
        _emailOptions = emailOptions.Value;
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

    public async Task<PasswordResetResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = (request.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new ValidationException("A valid email is required.");
        }

        if (!_email.IsConfigured)
        {
            throw new InvalidOperationException(
                "Email is not configured. Set Email:From, Email:ResetPasswordUrl, and either Email:Host (SMTP host, port, username, password) or Email:PickupDirectory.");
        }

        var user = await _users.FindByEmailAsync(email);
        if (user is not null && !user.IsRestricted)
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var link = BuildResetLink(email, token);
            try
            {
                await _email.SendAsync(
                    email,
                    "AE Hotel password reset",
                    $"<p>Use this link to reset your password. It expires after {_emailOptions.TokenLifespanHours} hours.</p><p><a href=\"{link}\">{link}</a></p>",
                    attachments: null,
                    cancellationToken);
            }
            catch (Exception exception) when (exception is not InvalidOperationException)
            {
                throw new InvalidOperationException("The password reset email could not be sent.", exception);
            }
        }

        return new PasswordResetResponse { Message = ForgotPasswordMessage };
    }

    public async Task<PasswordResetResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = (request.Email ?? string.Empty).Trim();
        var token = (request.Token ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            throw new ValidationException(InvalidResetMessage);
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ValidationException("Password is required.");
        }

        var user = await _users.FindByEmailAsync(email);
        if (user is null)
        {
            throw new ValidationException(InvalidResetMessage);
        }

        var reset = await _users.ResetPasswordAsync(user, token, request.NewPassword);
        if (!reset.Succeeded)
        {
            var decoded = WebUtility.UrlDecode(token).Trim();
            if (!string.Equals(decoded, token, StringComparison.Ordinal))
            {
                reset = await _users.ResetPasswordAsync(user, decoded, request.NewPassword);
            }
        }

        if (!reset.Succeeded)
        {
            if (reset.Errors.Any(error => string.Equals(error.Code, "InvalidToken", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ValidationException(InvalidResetMessage);
            }

            throw new ValidationException(string.Join(" ", reset.Errors.Select(error => error.Description)));
        }

        return new PasswordResetResponse
        {
            Message = "Password has been reset. You can sign in with the new password."
        };
    }

    private string BuildResetLink(string email, string token)
    {
        var baseUrl = _emailOptions.ResetPasswordUrl.Trim();
        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{baseUrl}{separator}email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
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
            Roles = roles.ToList(),
            AssignedHotelId = user.AssignedHotelId
        };
    }
}
