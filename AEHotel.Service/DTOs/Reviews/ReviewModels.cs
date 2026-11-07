namespace AEHotel.Service.DTOs.Reviews;

public class ReviewRequest
{
    public int HotelId { get; set; }

    public int BookingId { get; set; }

    public int Rating { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;
}

public class ReviewResponse
{
    public int Id { get; set; }

    public int HotelId { get; set; }

    public int? BookingId { get; set; }

    public string GuestName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;

    public bool IsApproved { get; set; }

    public string HotelName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class ReviewSummaryResponse
{
    public int Count { get; set; }

    public double AverageRating { get; set; }

    // Share of approved reviews rated 4 or 5, 0-100.
    public int PositivePercent { get; set; }
}
