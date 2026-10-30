namespace AEHotel.Service.DTOs.Payments;

public class PaymentStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class PaymentResponse
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public string BookingCode { get; set; } = string.Empty;

    public string Method { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int DepositPercent { get; set; }

    public decimal DepositAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string CardLast4 { get; set; } = string.Empty;

    public string CardHolder { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
