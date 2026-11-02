using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

public class TouristPlace : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public int CityId { get; set; }

    public City? City { get; set; }
}
