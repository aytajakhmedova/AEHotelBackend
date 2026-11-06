namespace AEHotel.Service.DTOs.Admin;

public class CustomerResponse
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public bool IsRestricted { get; set; }

    public DateTime CreatedAt { get; set; }

    public int BookingCount { get; set; }

    public decimal TotalSpent { get; set; }

    public DateTime? LastBookingAt { get; set; }
}

public class CustomerBookingResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string BookingType { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateOnly? CheckIn { get; set; }

    public DateOnly? CheckOut { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class StaffResponse
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string JobTitle { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int? AssignedHotelId { get; set; }

    public string AssignedHotelName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class StaffRequest
{
    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string JobTitle { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int? AssignedHotelId { get; set; }

    public bool IsActive { get; set; } = true;

    // required when creating, optional (leave empty to keep) when editing
    public string? Password { get; set; }
}

public class ReportResponse
{
    public DateOnly From { get; set; }

    public DateOnly To { get; set; }

    public decimal TotalRevenue { get; set; }

    public int BookingCount { get; set; }

    public int CancelledCount { get; set; }

    public decimal AverageBookingValue { get; set; }

    public List<ReportMonthRow> Monthly { get; set; } = [];

    public List<ReportCountRow> ByStatus { get; set; } = [];

    public List<ReportTypeRow> ByType { get; set; } = [];

    public List<ReportHotelRow> Hotels { get; set; } = [];

    public List<ReportPaymentRow> PaymentsByStatus { get; set; } = [];

    public List<ReportPaymentRow> PaymentsByMethod { get; set; } = [];
}

public class ReportMonthRow
{
    public string Month { get; set; } = string.Empty;

    public decimal Revenue { get; set; }

    public int Bookings { get; set; }
}

public class ReportCountRow
{
    public string Label { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class ReportTypeRow
{
    public string Type { get; set; } = string.Empty;

    public int Bookings { get; set; }

    public decimal Revenue { get; set; }
}

public class ReportHotelRow
{
    public int HotelId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Bookings { get; set; }

    public decimal Revenue { get; set; }

    public int Rooms { get; set; }

    public int BookedNights { get; set; }

    public decimal OccupancyPercent { get; set; }
}

public class ReportPaymentRow
{
    public string Label { get; set; } = string.Empty;

    public int Count { get; set; }

    public decimal Amount { get; set; }
}
