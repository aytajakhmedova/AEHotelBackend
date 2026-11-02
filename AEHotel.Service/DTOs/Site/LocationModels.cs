namespace AEHotel.Service.DTOs.Site;

public class CountryOptionResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string IsoCode { get; set; } = string.Empty;
}

public class CityOptionResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CountryId { get; set; }
}

public class TouristPlaceOptionResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CityId { get; set; }
}
