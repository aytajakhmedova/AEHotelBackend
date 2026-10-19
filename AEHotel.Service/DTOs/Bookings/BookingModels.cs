using System.Text.Json.Serialization;

namespace AEHotel.Service.DTOs.Bookings;

public class BookingRequest
{
    public string BookingType { get; set; } = "hotel";

    public int? HotelId { get; set; }

    public int? RoomId { get; set; }

    public int? FlightId { get; set; }

    public int? TourId { get; set; }

    public string GuestName { get; set; } = string.Empty;

    public string GuestEmail { get; set; } = string.Empty;

    public string? GuestPhone { get; set; }

    public DateOnly? CheckIn { get; set; }

    public DateOnly? CheckOut { get; set; }

    public DateOnly? TravelDate { get; set; }

    public int Guests { get; set; } = 1;

    public string? SpecialRequests { get; set; }

    public string PaymentMethod { get; set; } = "card";

    public string? CardNumber { get; set; }

    public string? CardHolder { get; set; }
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

    public string ItemName { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string SpecialRequests { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public int? PaymentId { get; set; }

    public DateTime CreatedAt { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TripSuggestionResponse? TripSuggestions { get; set; }
}
