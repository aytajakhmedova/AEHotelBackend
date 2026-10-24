using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

public class DiscountPackage : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Combo { get; set; } = string.Empty;

    public decimal Percent { get; set; }

    public decimal Amount { get; set; }

    public DateTime? StartsAt { get; set; }

    public DateTime? EndsAt { get; set; }

    public bool IsActive { get; set; } = true;
}
