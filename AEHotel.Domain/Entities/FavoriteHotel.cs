using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

/// <summary>A hotel a signed-in user has marked as a favourite.</summary>
public class FavoriteHotel : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    public int HotelId { get; set; }

    public Hotel? Hotel { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
