namespace AEHotel.App.Hotels;

public class HotelRequest
{
    public string Title { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public int Stars { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal PricePerNight { get; set; }

    public string HotelType { get; set; } = string.Empty;

    public string CheckInTime { get; set; } = "14:00";

    public string CheckOutTime { get; set; } = "12:00";
}
