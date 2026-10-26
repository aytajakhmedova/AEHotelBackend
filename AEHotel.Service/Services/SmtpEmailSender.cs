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

    public bool DeliversToInbox => !string.IsNullOrWhiteSpace(_options.Host);

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

        var recipient = ResolveRecipient(to);
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
            Directory.CreateDirectory(_options.PickupDirectory);
            await message.WriteToAsync(Path.Combine(_options.PickupDirectory, $"{Guid.NewGuid():N}.eml"), cancellationToken);
            _logger.LogInformation("Email written to pickup for {To}.", recipient);
            return;
        }

        using var client = new SmtpClient();
        var secure = _options.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        await client.ConnectAsync(_options.Host, _options.Port, secure, cancellationToken);
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

    private string ResolveRecipient(string to)
    {
        if (DeliversToInbox &&
            to.EndsWith("@aehotel.com", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(_options.From))
        {
            _logger.LogInformation("Demo address {Original} redirected to {To}.", to, _options.From);
            return _options.From;
        }

        return to;
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
