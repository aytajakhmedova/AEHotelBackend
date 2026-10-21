using AEHotel.Service.DTOs.Flights;
using AEHotel.Service.DTOs.Tours;

namespace AEHotel.Service.DTOs.Bookings;

public class TripSuggestionResponse
{
    public string Message { get; set; } = "Səyahətini tamamla — uçuş və tur seçimlərinə də bax!";

    public bool Required { get; set; }

    public IReadOnlyList<FlightResponse> Flights { get; set; } = [];

    public IReadOnlyList<TourResponse> Tours { get; set; } = [];
}
