using System.Net.Security;
using AEHotel.Service.DTOs.Email;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AEHotel.Service.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
        _logger.LogInformation(
            "Email sender ready. Host={Host} User={User} PasswordSet={PasswordSet} Inbox={Inbox}",
            _options.Host,
            _options.UserName,
            !string.IsNullOrWhiteSpace(_options.Password),
            DeliversToInbox);
    }

    public bool IsConfigured => _options.IsConfigured;

    public bool DeliversToInbox => _options.DeliversToInbox;

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        IReadOnlyList<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Email is not configured. Set Email:From, Email:ResetPasswordUrl, and either Email:Host (SMTP) or Email:PickupDirectory.");
        }

        if (string.IsNullOrWhiteSpace(to) || !to.Contains('@'))
        {
            throw new InvalidOperationException("Qonaq email real ünvan olmalıdır.");
        }

        var recipient = to.Trim();
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.From));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = subject;

        var body = new BodyBuilder { HtmlBody = htmlBody };
        foreach (var attachment in attachments ?? [])
        {
            if (attachment.Content is not { Length: > 0 } || string.IsNullOrWhiteSpace(attachment.FileName))
            {
                continue;
            }

            body.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
        }

        message.Body = body.ToMessageBody();

        if (!DeliversToInbox)
        {
            throw new InvalidOperationException("Gmail SMTP şifrəsi yüklənməyib. appsettings.Local.json və ya user-secrets lazımdır.");
        }

        using var client = new SmtpClient();
        client.CheckCertificateRevocation = false;
        client.ServerCertificateValidationCallback = (_, _, _, errors) =>
            errors == SslPolicyErrors.None
            || string.Equals(_options.Host, "smtp.gmail.com", StringComparison.OrdinalIgnoreCase);

        await ConnectAsync(client, cancellationToken);
        client.AuthenticationMechanisms.Remove("XOAUTH2");
        client.AuthenticationMechanisms.Remove("XOAUTH");
        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            await AuthenticateAsync(client, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
        _logger.LogInformation("Email sent to {To} via SMTP.", recipient);
    }

    private async Task ConnectAsync(SmtpClient client, CancellationToken cancellationToken)
    {
        var host = _options.Host;
        var port = _options.Port <= 0 ? 587 : _options.Port;
        try
        {
            var secure = port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await client.ConnectAsync(host, port, secure, cancellationToken);
        }
        catch (Exception exception) when (port != 465)
        {
            _logger.LogWarning(exception, "SMTP {Port} failed, retrying 465.", port);
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, cancellationToken);
            }

            await client.ConnectAsync(host, 465, SecureSocketOptions.SslOnConnect, cancellationToken);
        }
    }

    private async Task AuthenticateAsync(SmtpClient client, CancellationToken cancellationToken)
    {
        var password = _options.Password ?? string.Empty;
        try
        {
            await client.AuthenticateAsync(_options.UserName, password.Replace(" ", string.Empty), cancellationToken);
        }
        catch (AuthenticationException) when (password.Contains(' '))
        {
            await client.AuthenticateAsync(_options.UserName, password, cancellationToken);
        }
    }
}
