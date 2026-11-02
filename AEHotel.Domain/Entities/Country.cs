using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

public class Country : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>ISO 3166-1 alpha-2 code, used by the frontend to pick the flag.</summary>
    public string IsoCode { get; set; } = string.Empty;

    public ICollection<City> Cities { get; set; } = new List<City>();
}
