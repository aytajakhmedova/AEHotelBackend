using AEHotel.Service.DTOs.Flights;
using AEHotel.Service.DTOs.Tours;

namespace AEHotel.Service.DTOs.Bookings;

public class TripSuggestionResponse
{
    public string Message { get; set; } = "Səyahətini tamamla — uçuş və tur seçimlərinə də bax!";

    public bool Required { get; set; }

    /// <summary>Flights going to the country (the outbound leg).</summary>
    public IReadOnlyList<FlightResponse> Flights { get; set; } = [];

    /// <summary>Flights leaving the country (the return leg of a round trip).</summary>
    public IReadOnlyList<FlightResponse> ReturnFlights { get; set; } = [];

    public IReadOnlyList<TourResponse> Tours { get; set; } = [];
}

public class TripCountryResponse
{
    public string Name { get; set; } = string.Empty;

    public string IsoCode { get; set; } = string.Empty;
}
