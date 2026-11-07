using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

public class DiscountPackage : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Combo { get; set; } = string.Empty;

    public string? Code { get; set; }

    public decimal Percent { get; set; }

    public decimal Amount { get; set; }

    public DateTime? StartsAt { get; set; }

    public DateTime? EndsAt { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Owner of a personal coupon (for example the welcome coupon); null for coupons anyone may use.</summary>
    public string? UserId { get; set; }

    /// <summary>When a personal coupon was used for the first time; it works once.</summary>
    public DateTime? UsedAt { get; set; }
}
