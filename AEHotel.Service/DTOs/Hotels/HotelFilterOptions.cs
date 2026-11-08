namespace AEHotel.Service.DTOs.Hotels;

/// <summary>The values the hotel filter can offer: only what the hotels in the database really have.</summary>
public class HotelFilterOptions
{
    public IReadOnlyList<string> HotelTypes { get; set; } = [];

    public IReadOnlyList<string> Amenities { get; set; } = [];
}
