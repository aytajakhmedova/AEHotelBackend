using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

public class Flight : BaseEntity
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

    public decimal Price { get; set; }

    public string CabinClass { get; set; } = "economy";

    public int SeatsAvailable { get; set; }

    public bool Refundable { get; set; }
}
