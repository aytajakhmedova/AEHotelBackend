using System.Net;
using System.Net.Mail;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AEHotel.Service.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Email is not configured. Set Email:From, Email:ResetPasswordUrl, and either Email:Host (SMTP) or Email:PickupDirectory.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.From),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = CreateClient();
        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Password reset email queued for delivery.");
    }

    private SmtpClient CreateClient()
    {
        if (!string.IsNullOrWhiteSpace(_options.PickupDirectory))
        {
            Directory.CreateDirectory(_options.PickupDirectory);
            return new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = _options.PickupDirectory
            };
        }

        var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
        }

        return client;
    }
}
