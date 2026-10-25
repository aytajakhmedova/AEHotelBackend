using AEHotel.Service.DTOs.Rooms;

namespace AEHotel.Service.DTOs.Hotels;

public class HotelResponse
{
    public int Id { get; set; }

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

    public string CheckInTime { get; set; } = string.Empty;

    public string CheckOutTime { get; set; } = string.Empty;

    public string CancellationPolicy { get; set; } = string.Empty;

    public string ImageFile { get; set; } = string.Empty;

    public IReadOnlyList<string> Amenities { get; set; } = [];

    public IReadOnlyList<RoomResponse> Rooms { get; set; } = [];
}
