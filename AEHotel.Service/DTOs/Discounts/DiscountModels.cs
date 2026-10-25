namespace AEHotel.Service.DTOs.Discounts;

public class DiscountPackageRequest
{
    public string Name { get; set; } = string.Empty;

    public string Combo { get; set; } = string.Empty;

    public decimal Percent { get; set; }

    public decimal Amount { get; set; }

    public DateTime? StartsAt { get; set; }

    public DateTime? EndsAt { get; set; }

    public bool IsActive { get; set; } = true;
}

public class DiscountPackageResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Combo { get; set; } = string.Empty;

    public decimal Percent { get; set; }

    public decimal Amount { get; set; }

    public DateTime? StartsAt { get; set; }

    public DateTime? EndsAt { get; set; }

    public bool IsActive { get; set; }
}
