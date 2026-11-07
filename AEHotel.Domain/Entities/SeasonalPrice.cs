using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

/// <summary>Typical nightly price range of a country for one month, shown on the home page.</summary>
public class SeasonalPrice : BaseEntity
{
    public string Country { get; set; } = string.Empty;

    /// <summary>1 (January) to 12 (December).</summary>
    public int Month { get; set; }

    public decimal MinPrice { get; set; }

    public decimal MaxPrice { get; set; }

    /// <summary>AZN, TRY, EUR or USD.</summary>
    public string Currency { get; set; } = "AZN";
}
