using AEHotel.Domain.Entities;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.DTOs.Bookings;
using AEHotel.Service.DTOs.Flights;
using AEHotel.Service.DTOs.Tours;
using AEHotel.Service.Helpers;
using AEHotel.Service.Services.Interfaces;

namespace AEHotel.Service.Services;

public class TripSuggestionService : ITripSuggestionService
{
    private const int Limit = 5;

    private readonly IFlightRepository _flights;
    private readonly ITourRepository _tours;

    public TripSuggestionService(IFlightRepository flights, ITourRepository tours)
    {
        _flights = flights;
        _tours = tours;
    }

    public async Task<TripSuggestionResponse> GetAsync(
        string? city,
        DateOnly? checkIn,
        DateOnly? checkOut,
        int guests = 1,
        CancellationToken cancellationToken = default)
    {
        var passengerCount = guests < 1 ? 1 : guests;
        var flights = await _flights.GetAllAsync(cancellationToken);
        var tours = await _tours.GetAllAsync(cancellationToken);

        return new TripSuggestionResponse
        {
            Message = "Səyahətini tamamla — uçuş və tur seçimlərinə də bax!",
            Required = false,
            Flights = PickFlights(flights, city, checkIn, checkOut, passengerCount),
            Tours = PickTours(tours, city, checkIn, checkOut, passengerCount)
        };
    }

    private static IReadOnlyList<FlightResponse> PickFlights(
        IReadOnlyList<Flight> flights,
        string? city,
        DateOnly? checkIn,
        DateOnly? checkOut,
        int guests)
    {
        var open = flights.Where(flight => flight.SeatsAvailable >= guests).ToList();
        var inbound = open
            .Where(flight => MatchesPlace(city, flight.DestinationCity, flight.DestinationCode) &&
                             (!checkIn.HasValue || DateOnly.FromDateTime(flight.DepartureAt) == checkIn.Value))
            .ToList();
        var outbound = open
            .Where(flight => MatchesPlace(city, flight.OriginCity, flight.OriginCode) &&
                             (!checkOut.HasValue || DateOnly.FromDateTime(flight.DepartureAt) == checkOut.Value))
            .ToList();
        var nearby = open
            .Where(flight => MatchesPlace(city, flight.DestinationCity, flight.DestinationCode) ||
                             MatchesPlace(city, flight.OriginCity, flight.OriginCode))
            .ToList();

        return inbound
            .Concat(outbound)
            .Concat(nearby)
            .Concat(open.OrderBy(flight => flight.Price))
            .DistinctBy(flight => flight.Id)
            .Take(Limit)
            .Select(MapFlight)
            .ToList();
    }

    private static IReadOnlyList<TourResponse> PickTours(
        IReadOnlyList<Tour> tours,
        string? city,
        DateOnly? checkIn,
        DateOnly? checkOut,
        int guests)
    {
        var matching = tours
            .Where(tour => guests >= tour.MinGuests && guests <= tour.MaxGuests)
            .Where(tour => MatchesPlace(city, tour.Destination, tour.Name) && FitsStay(tour, checkIn, checkOut))
            .ToList();
        var fallback = tours
            .Where(tour => guests >= tour.MinGuests && guests <= tour.MaxGuests)
            .ToList();

        return matching
            .Concat(fallback)
            .DistinctBy(tour => tour.Id)
            .Take(Limit)
            .Select(MapTour)
            .ToList();
    }

    private static bool FitsStay(Tour tour, DateOnly? checkIn, DateOnly? checkOut)
    {
        var dates = JsonLists.ReadStrings(tour.AvailableDatesJson);
        if (dates.Count == 0 || !checkIn.HasValue)
        {
            return true;
        }

        return dates.Any(value =>
            DateOnly.TryParse(value, out var date) &&
            date >= checkIn.Value &&
            (!checkOut.HasValue || date < checkOut.Value));
    }

    private static bool MatchesPlace(string? query, params string[] values)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        var needle = query.Trim();
        return values.Any(value =>
            !string.IsNullOrWhiteSpace(value) &&
            (value.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
             needle.Contains(value, StringComparison.OrdinalIgnoreCase)));
    }

    private static FlightResponse MapFlight(Flight flight)
    {
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
            Price = flight.Price,
            CabinClass = flight.CabinClass,
            SeatsAvailable = flight.SeatsAvailable,
            Refundable = flight.Refundable
        };
    }

    private static TourResponse MapTour(Tour tour)
    {
        return new TourResponse
        {
            Id = tour.Id,
            Name = tour.Name,
            Destination = tour.Destination,
            Rating = tour.Rating,
            ReviewCount = tour.ReviewCount,
            Duration = tour.Duration,
            Price = tour.Price,
            ImageFile = tour.ImageFile,
            Description = tour.Description,
            Category = tour.Category,
            Difficulty = tour.Difficulty,
            MaxGuests = tour.MaxGuests,
            MinGuests = tour.MinGuests,
            Highlights = JsonLists.ReadStrings(tour.HighlightsJson),
            Included = JsonLists.ReadStrings(tour.IncludedJson),
            Excluded = JsonLists.ReadStrings(tour.ExcludedJson),
            Itinerary = JsonLists.Read<TourDayResponse>(tour.ItineraryJson),
            AvailableDates = JsonLists.ReadStrings(tour.AvailableDatesJson)
        };
    }
}
