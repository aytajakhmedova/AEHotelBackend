using AEHotel.Service.DTOs.Email;

namespace AEHotel.Service.Services.Interfaces;

public interface IEmailSender
{
    bool IsConfigured { get; }

    bool DeliversToInbox { get; }

    Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        IReadOnlyList<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default);
}
