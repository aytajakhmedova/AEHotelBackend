namespace AEHotel.Service.DTOs.Tours;

public class TourCategoryRequest
{
    public string Name { get; set; } = string.Empty;
}

public class TourCategoryResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>How many tours currently use this category.</summary>
    public int TourCount { get; set; }
}
