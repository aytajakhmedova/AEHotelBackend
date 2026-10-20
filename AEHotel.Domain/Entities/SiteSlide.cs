using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

public class SiteSlide : BaseEntity
{
    public string ImageUrl { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
