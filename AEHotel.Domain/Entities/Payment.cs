using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

public class Payment : BaseEntity
{

    public int BookingId { get; set; }

    public Booking? Booking { get; set; }

    public string Method { get; set; } = "card";

    public decimal Amount { get; set; }

    public int DepositPercent { get; set; }

    public decimal DepositAmount { get; set; }

    public string Status { get; set; } = "pending";

    public string CardLast4 { get; set; } = string.Empty;

    public string CardHolder { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
