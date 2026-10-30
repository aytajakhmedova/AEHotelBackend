namespace AEHotel.Service.Security;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string From { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool EnableSsl { get; set; } = true;

    public string PickupDirectory { get; set; } = string.Empty;

    public string ResetPasswordUrl { get; set; } = string.Empty;

    public int TokenLifespanHours { get; set; } = 2;

    public bool DeliversToInbox =>
        !string.IsNullOrWhiteSpace(From)
        && !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(UserName)
        && !string.IsNullOrWhiteSpace(Password);

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(From)
        && !string.IsNullOrWhiteSpace(ResetPasswordUrl)
        && (DeliversToInbox || !string.IsNullOrWhiteSpace(PickupDirectory));
}
