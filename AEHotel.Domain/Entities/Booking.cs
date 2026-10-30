using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

public class Booking : BaseEntity
{

    public string Code { get; set; } = string.Empty;

    public string BookingType { get; set; } = "hotel";

    public string Status { get; set; } = "confirmed";

    public int? HotelId { get; set; }

    public Hotel? Hotel { get; set; }

    public int? RoomId { get; set; }

    public Room? Room { get; set; }

    public int? FlightId { get; set; }

    public Flight? Flight { get; set; }

    public int? TourId { get; set; }

    public Tour? Tour { get; set; }

    public string? UserId { get; set; }

    public AppUser? User { get; set; }

    public string GuestName { get; set; } = string.Empty;

    public string GuestEmail { get; set; } = string.Empty;

    public string GuestPhone { get; set; } = string.Empty;

    public DateOnly? CheckIn { get; set; }

    public DateOnly? CheckOut { get; set; }

    public int Guests { get; set; }

    public int Nights { get; set; }

    public decimal PricePerNight { get; set; }

    public decimal TotalPrice { get; set; }

    public string CouponCode { get; set; } = string.Empty;

    public decimal DiscountAmount { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string SpecialRequests { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Payment? Payment { get; set; }
}
