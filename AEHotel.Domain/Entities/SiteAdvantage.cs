using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

public class SiteAdvantage : BaseEntity
{
    public string IconKey { get; set; } = string.Empty;

    public string Color { get; set; } = "#4ade80";

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
