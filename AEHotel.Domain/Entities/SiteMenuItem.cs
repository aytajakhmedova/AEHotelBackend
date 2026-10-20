using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

public class SiteMenuItem : BaseEntity
{
    public string Label { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Placement { get; set; } = "footer";

    public string IconKey { get; set; } = string.Empty;

    public bool IsRoute { get; set; } = true;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
