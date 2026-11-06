namespace AEHotel.Domain.Entities;

public class BookingRoom
{
    public int BookingId { get; set; }
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public int Adults { get; set; }
    public int Children { get; set; }
    public decimal PricePerNight { get; set; }
}
