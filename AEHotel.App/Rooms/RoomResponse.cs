namespace AEHotel.App.Rooms;

public class RoomResponse
{
    public int Id { get; set; }

    public int HotelId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public string BedType { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public decimal PricePerNight { get; set; }

    public bool Available { get; set; }
}
