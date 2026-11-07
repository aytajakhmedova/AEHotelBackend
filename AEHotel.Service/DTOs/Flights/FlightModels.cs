namespace AEHotel.Service.DTOs.Flights;

public class FlightCabinDto
{
    /// <summary>economy, business or first.</summary>
    public string CabinClass { get; set; } = "economy";

    public decimal Price { get; set; }

    public int SeatsAvailable { get; set; }
}

public class FlightRequest
{
    public string Airline { get; set; } = string.Empty;

    public string FlightNumber { get; set; } = string.Empty;

    public string OriginCode { get; set; } = string.Empty;

    public string OriginCity { get; set; } = string.Empty;

    public string OriginCountry { get; set; } = string.Empty;

    public string DestinationCode { get; set; } = string.Empty;

    public string DestinationCity { get; set; } = string.Empty;

    public string DestinationCountry { get; set; } = string.Empty;

    public DateTime DepartureAt { get; set; }

    public DateTime ArrivalAt { get; set; }

    public string Duration { get; set; } = string.Empty;

    public int Stops { get; set; }

    public bool Refundable { get; set; }

    /// <summary>The classes sold on this flight, each with its own price and seats.</summary>
    public List<FlightCabinDto> Cabins { get; set; } = [];
}

public class FlightResponse : FlightRequest
{
    public int Id { get; set; }

    /// <summary>Price of the class searched for, or the cheapest class.</summary>
    public decimal Price { get; set; }

    public string CabinClass { get; set; } = "economy";

    public int SeatsAvailable { get; set; }
}

public class AirportResponse
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    // Number of matching flights that depart from or land at this airport.
    public int Count { get; set; }
}

public class PopularDestinationResponse
{
    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public int FlightCount { get; set; }

    public decimal MinPrice { get; set; }

    public DateTime NextDeparture { get; set; }
}
