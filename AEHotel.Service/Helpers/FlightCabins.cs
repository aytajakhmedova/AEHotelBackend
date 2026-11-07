using AEHotel.Domain.Entities;
using AEHotel.Service.DTOs.Flights;

namespace AEHotel.Service.Helpers;

/// <summary>Rules and mapping for the travel classes of a flight.</summary>
internal static class FlightCabins
{
    public static readonly string[] Classes = ["economy", "business", "first"];

    /// <summary>The canonical class name for an input such as "Business", or null if it is not a class.</summary>
    public static string? Normalize(string? cabin) =>
        Classes.FirstOrDefault(item => string.Equals(item, (cabin ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase));

    private static bool Is(FlightCabin cabin, string? name) =>
        string.IsNullOrWhiteSpace(name) || string.Equals(cabin.CabinClass, name.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the flight (in the given class, or any class) has room for that many passengers.</summary>
    public static bool HasSeats(Flight flight, int passengers, string? cabin = null) =>
        flight.Cabins.Any(item => Is(item, cabin) && item.SeatsAvailable >= passengers);

    public static decimal MinPrice(Flight flight) =>
        flight.Cabins.Count > 0 ? flight.Cabins.Min(item => item.Price) : flight.Price;

    public static FlightResponse Map(Flight flight, string? cabin = null)
    {
        var ordered = flight.Cabins.OrderBy(item => Array.IndexOf(Classes, item.CabinClass)).ToList();
        // The class shown first: the one searched for, otherwise the cheapest.
        var selected = ordered.FirstOrDefault(item => !string.IsNullOrWhiteSpace(cabin) && Is(item, cabin))
            ?? ordered.OrderBy(item => item.Price).FirstOrDefault();

        return new FlightResponse
        {
            Id = flight.Id,
            Airline = flight.Airline,
            FlightNumber = flight.FlightNumber,
            OriginCode = flight.OriginCode,
            OriginCity = flight.OriginCity,
            OriginCountry = flight.OriginCountry,
            DestinationCode = flight.DestinationCode,
            DestinationCity = flight.DestinationCity,
            DestinationCountry = flight.DestinationCountry,
            DepartureAt = flight.DepartureAt,
            ArrivalAt = flight.ArrivalAt,
            Duration = flight.Duration,
            Stops = flight.Stops,
            Refundable = flight.Refundable,
            Cabins = ordered.Select(item => new FlightCabinDto
            {
                CabinClass = item.CabinClass,
                Price = item.Price,
                SeatsAvailable = item.SeatsAvailable
            }).ToList(),
            Price = selected?.Price ?? flight.Price,
            CabinClass = selected?.CabinClass ?? flight.CabinClass,
            SeatsAvailable = selected?.SeatsAvailable ?? flight.SeatsAvailable
        };
    }
}
