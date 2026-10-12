using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

public class Tour : BaseEntity
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

    public string HighlightsJson { get; set; } = "[]";

    public string IncludedJson { get; set; } = "[]";

    public string ExcludedJson { get; set; } = "[]";

    public string ItineraryJson { get; set; } = "[]";

    public string AvailableDatesJson { get; set; } = "[]";
}
