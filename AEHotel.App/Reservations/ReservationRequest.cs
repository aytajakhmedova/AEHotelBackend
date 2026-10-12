namespace AEHotel.App.Reservations;

public class ReservationRequest
{
    public int HotelId { get; set; }

    public int RoomId { get; set; }

    public string GuestName { get; set; } = string.Empty;

    public string GuestEmail { get; set; } = string.Empty;

    public DateOnly CheckIn { get; set; }

    public DateOnly CheckOut { get; set; }

    public int Guests { get; set; }

    public string? Status { get; set; }
}
