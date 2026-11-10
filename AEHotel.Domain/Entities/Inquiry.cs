using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

/// <summary>A question a guest sends. With a HotelId it goes to that hotel's manager; without one it goes to the admins.</summary>
public class Inquiry : BaseEntity
{
    public int? HotelId { get; set; }

    public Hotel? Hotel { get; set; }

    /// <summary>Set when the guest was signed in, so they can see the answer under "my questions".</summary>
    public string? UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>new, answered or closed.</summary>
    public string Status { get; set; } = "new";

    public string Reply { get; set; } = string.Empty;

    public string RepliedBy { get; set; } = string.Empty;

    public DateTime? RepliedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
