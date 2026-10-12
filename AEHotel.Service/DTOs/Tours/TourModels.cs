namespace AEHotel.Service.DTOs.Tours;

public class TourDayResponse
{
    public int Day { get; set; }

    public string Title { get; set; } = string.Empty;

    public List<string> Activities { get; set; } = [];
}

public class TourRequest
{
    public string Name { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public decimal Rating { get; set; }

    public int ReviewCount { get; set; }

    public string Duration { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string ImageFile { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Difficulty { get; set; } = string.Empty;

    public int MaxGuests { get; set; }

    public int MinGuests { get; set; } = 1;

    public List<string> Highlights { get; set; } = [];

    public List<string> Included { get; set; } = [];

    public List<string> Excluded { get; set; } = [];

    public List<TourDayResponse> Itinerary { get; set; } = [];

    public List<string> AvailableDates { get; set; } = [];
}

public class TourResponse : TourRequest
{
    public int Id { get; set; }
}
