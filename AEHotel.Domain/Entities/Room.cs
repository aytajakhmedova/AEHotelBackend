namespace AEHotel.Domain.Entities;

public class Room
{
    public int Id { get; set; }

    public int HotelId { get; set; }

    public Hotel? Hotel { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public string BedType { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public decimal PricePerNight { get; set; }

    public bool Available { get; set; } = true;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
