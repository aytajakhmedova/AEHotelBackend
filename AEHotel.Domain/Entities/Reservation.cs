namespace AEHotel.Domain.Entities;

public class Reservation
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Status { get; set; } = "confirmed";

    public int HotelId { get; set; }

    public Hotel? Hotel { get; set; }

    public int RoomId { get; set; }

    public Room? Room { get; set; }

    public string GuestName { get; set; } = string.Empty;

    public string GuestEmail { get; set; } = string.Empty;

    public DateOnly CheckIn { get; set; }

    public DateOnly CheckOut { get; set; }

    public int Guests { get; set; }

    public decimal PricePerNight { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; }
}
