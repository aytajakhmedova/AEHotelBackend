namespace AEHotel.App.Reservations;

public class ReservationResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int HotelId { get; set; }

    public string HotelName { get; set; } = string.Empty;

    public int RoomId { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public string GuestName { get; set; } = string.Empty;

    public string GuestEmail { get; set; } = string.Empty;

    public DateOnly CheckIn { get; set; }

    public DateOnly CheckOut { get; set; }

    public int Guests { get; set; }

    public int Nights { get; set; }

    public decimal PricePerNight { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; }
}
