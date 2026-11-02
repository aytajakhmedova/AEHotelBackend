namespace AEHotel.Service.DTOs.Hotels;

public class HotelRequest
{
    public string Title { get; set; } = string.Empty;

    /// <summary>Selected country, city and tourist place. All three are required and checked against each other.</summary>
    public int? CountryId { get; set; }

    public int? CityId { get; set; }

    public int? TouristPlaceId { get; set; }

    public string Address { get; set; } = string.Empty;

    public int Stars { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal PricePerNight { get; set; }

    public string HotelType { get; set; } = string.Empty;

    public string CheckInTime { get; set; } = "14:00";

    public string CheckOutTime { get; set; } = "12:00";

    public string CancellationPolicy { get; set; } = "Check-in tarixindən əvvəl ləğv etmək olar.";

    public string ImageFile { get; set; } = string.Empty;

    public List<string> Amenities { get; set; } = [];
}
