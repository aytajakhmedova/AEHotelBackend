namespace AEHotel.Service.DTOs.SeasonalPrices;

public class SeasonalPriceRequest
{
    public string Country { get; set; } = string.Empty;

    public int Month { get; set; }

    public decimal MinPrice { get; set; }

    public decimal MaxPrice { get; set; }

    public string Currency { get; set; } = "AZN";
}

public class SeasonalPriceResponse
{
    public int Id { get; set; }

    public string Country { get; set; } = string.Empty;

    public int Month { get; set; }

    public decimal MinPrice { get; set; }

    public decimal MaxPrice { get; set; }

    public string Currency { get; set; } = "AZN";
}
