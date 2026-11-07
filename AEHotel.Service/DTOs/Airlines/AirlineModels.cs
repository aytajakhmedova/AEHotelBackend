namespace AEHotel.Service.DTOs.Airlines;

public class AirlineRequest
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}

public class AirlineResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    /// <summary>How many flights currently use this airline.</summary>
    public int FlightCount { get; set; }
}
