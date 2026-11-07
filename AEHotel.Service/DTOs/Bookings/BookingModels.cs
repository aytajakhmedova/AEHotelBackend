using System.Text.Json.Serialization;

namespace AEHotel.Service.DTOs.Bookings;

public class HotelStayRequest
{
    public List<int>? RoomIds { get; set; }
    public int? Rooms { get; set; }
    public int? Adults { get; set; }
    public int? Children { get; set; }
    public int? RoomId { get; set; }
    public DateOnly? CheckIn { get; set; }
    public DateOnly? CheckOut { get; set; }
    public int Guests { get; set; } = 1;
}

public record StayRoom(int RoomId, string RoomName, int Adults, int Children, decimal PricePerNight, decimal Total);
public record HotelStayQuote(int Nights, decimal Total, IReadOnlyList<StayRoom> Rooms);

public class BookingRequest : HotelStayRequest
{
    public string BookingType { get; set; } = "hotel";

    public decimal? ExpectedStayTotal { get; set; }

    public int? HotelId { get; set; }

    public int? FlightId { get; set; }

    /// <summary>Travel class for a flight: economy, business or first.</summary>
    public string? CabinClass { get; set; }

    public int? TourId { get; set; }

    public string GuestName { get; set; } = string.Empty;

    public string GuestEmail { get; set; } = string.Empty;

    public string? GuestPhone { get; set; }

    public DateOnly? TravelDate { get; set; }

    public string? SpecialRequests { get; set; }

    public string PaymentMethod { get; set; } = "card";

    public string? CardNumber { get; set; }

    public string? CardHolder { get; set; }

    public int? DepositPercent { get; set; }

    public bool BundleWithHotel { get; set; }

    public string? BundleCombo { get; set; }

    public string? CouponCode { get; set; }

    /// <summary>Country picked on checkout for add-on flights and tours; the selected item must belong to it.</summary>
    public string? TravelCountry { get; set; }
}

public class BookingUpdateRequest
{
    public DateOnly? CheckIn { get; set; }

    public DateOnly? CheckOut { get; set; }

    public int Guests { get; set; }

    public string? SpecialRequests { get; set; }
}

public class BookingStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class BookingResponse
{
    public IReadOnlyList<StayRoom> Rooms { get; set; } = [];

    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string BookingType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int? HotelId { get; set; }

    public string HotelName { get; set; } = string.Empty;

    public int? RoomId { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public int? FlightId { get; set; }

    public string FlightNumber { get; set; } = string.Empty;

    public string CabinClass { get; set; } = string.Empty;

    public int? TourId { get; set; }

    public string TourName { get; set; } = string.Empty;

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

    public string PaymentMethod { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public int DepositPercent { get; set; }

    public decimal DepositAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public int? PaymentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool EmailSent { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TripSuggestionResponse? TripSuggestions { get; set; }
}
