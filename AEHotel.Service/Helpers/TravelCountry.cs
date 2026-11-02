using AEHotel.Domain.Entities;

namespace AEHotel.Service.Helpers;

public static class TravelCountry
{
    public static bool Same(string? left, string? right)
    {
        return !string.IsNullOrWhiteSpace(left) &&
               !string.IsNullOrWhiteSpace(right) &&
               string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static string OfTour(Tour tour)
    {
        var parts = tour.Destination.Split(',', 2, StringSplitOptions.TrimEntries);
        return parts.Length > 1 ? parts[1] : string.Empty;
    }

    public static string OfFlight(Flight flight)
    {
        return flight.DestinationCountry?.Trim() ?? string.Empty;
    }
}
