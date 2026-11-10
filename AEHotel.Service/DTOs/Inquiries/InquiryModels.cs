namespace AEHotel.Service.DTOs.Inquiries;

public class InquiryRequest
{
    /// <summary>The hotel the question is about; leave empty for a general question.</summary>
    public int? HotelId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

public class InquiryReplyRequest
{
    public string Reply { get; set; } = string.Empty;
}

public class InquiryStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class InquiryResponse
{
    public int Id { get; set; }

    public int? HotelId { get; set; }

    public string HotelName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Status { get; set; } = "new";

    public string Reply { get; set; } = string.Empty;

    public string RepliedBy { get; set; } = string.Empty;

    public DateTime? RepliedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
