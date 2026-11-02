using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

public class City : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public int CountryId { get; set; }

    public Country? Country { get; set; }
}
