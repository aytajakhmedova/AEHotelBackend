using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

public class Room : BaseEntity
{

    public int HotelId { get; set; }

    public Hotel? Hotel { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public string BedType { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public decimal PricePerNight { get; set; }

    public bool Available { get; set; } = true;

    public string HousekeepingStatus { get; set; } = "ready";

    public string ImageFile { get; set; } = string.Empty;

    public string AmenitiesJson { get; set; } = "[]";

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
