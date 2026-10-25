using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

public class Hotel : BaseEntity
{

    public string Title { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public int Stars { get; set; }

    public decimal Rating { get; set; }

    public int ReviewCount { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal PricePerNight { get; set; }

    public string HotelType { get; set; } = string.Empty;

    public string CheckInTime { get; set; } = "14:00";

    public string CheckOutTime { get; set; } = "12:00";

    public string CancellationPolicy { get; set; } = "Check-in tarixindən əvvəl ləğv etmək olar.";

    public string ImageFile { get; set; } = string.Empty;

    public string AmenitiesJson { get; set; } = "[]";

    public ICollection<Room> Rooms { get; set; } = new List<Room>();

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
