using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

/// <summary>A category an admin can pick for a tour (and guests can filter tours by).</summary>
public class TourCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}
