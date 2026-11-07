using System.Net;
using AEHotel.Domain.Entities;
using AEHotel.Service.DTOs.Auth;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
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
    private readonly IDiscountService _discounts;
    private readonly ILogger<AuthService> _logger;

    // Every new customer gets this discount as a personal coupon by email.
    private const decimal WelcomePercent = 25;
    private const int WelcomeValidMinutes = 5;

    public AuthService(
        UserManager<AppUser> users,
        IJwtTokenGenerator tokens,
        IEmailSender email,
        IOptions<EmailOptions> emailOptions,
        IDiscountService discounts,
        ILogger<AuthService> logger)
    {
        _discounts = discounts;
        _logger = logger;
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
        await SendWelcomeCouponAsync(user, cancellationToken);
        return await IssueTokenAsync(user);
    }

    // The coupon is created first and mailed second; a mail problem must never block the registration.
    private async Task SendWelcomeCouponAsync(AppUser user, CancellationToken cancellationToken)
    {
        try
        {
            var coupon = await _discounts.CreateWelcomeCouponAsync(user.Id, WelcomePercent, WelcomeValidMinutes, cancellationToken);
            if (!_email.IsConfigured)
            {
                _logger.LogWarning("Welcome coupon {Code} was created for {Email} but email is not configured.", coupon.Code, user.Email);
                return;
            }

            var siteUrl = Uri.TryCreate(_emailOptions.ResetPasswordUrl, UriKind.Absolute, out var uri)
                ? uri.GetLeftPart(UriPartial.Authority)
                : string.Empty;
            var until = coupon.EndsAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? string.Empty;
            var button = siteUrl.Length > 0
                ? $"<p><a href=\"{siteUrl}\" style=\"display:inline-block;padding:12px 22px;background:#8B5A3C;color:#fff;border-radius:8px;text-decoration:none\">Sayta keç</a></p>"
                : string.Empty;
            var body =
                $"<div style=\"font-family:Arial,sans-serif;max-width:480px;margin:auto\">" +
                $"<h2>Xoş gəlmisiniz, {WebUtility.HtmlEncode(user.Name)}!</h2>" +
                $"<p>AE Hotel-də qeydiyyatdan keçdiyiniz üçün sizə <strong>{(int)WelcomePercent}% endirim</strong> kuponu hədiyyə edirik.</p>" +
                $"<p style=\"font-size:24px;letter-spacing:2px;background:#f4ece6;padding:14px;text-align:center;border-radius:8px\"><strong>{coupon.Code}</strong></p>" +
                $"<p>Kuponu rezervasiya zamanı ödəniş səhifəsində kupon xanasına yazın. Yalnız sizin üçündür, bir dəfə işləyir və <strong>{WelcomeValidMinutes} dəqiqə</strong> etibarlıdır ({until} tarixinədək).</p>" +
                button +
                "</div>";
            // A slow mail server must not hold the registration for long.
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            await _email.SendAsync(user.Email!, $"AE Hotel: {(int)WelcomePercent}% endirim kuponunuz", body, cancellationToken: timeout.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Welcome coupon for {Email} could not be created or sent.", user.Email);
        }
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
