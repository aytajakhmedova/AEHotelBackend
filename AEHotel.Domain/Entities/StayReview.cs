using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

public class StayReview : BaseEntity
{
    public int HotelId { get; set; }

    public Hotel? Hotel { get; set; }

    public int? BookingId { get; set; }

    public Booking? Booking { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string GuestName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;

    public bool IsApproved { get; set; }

    public DateTime CreatedAt { get; set; }
}
