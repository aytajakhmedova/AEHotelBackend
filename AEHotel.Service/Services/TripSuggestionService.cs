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
    private readonly IHotelRepository _hotels;
    private readonly ILocationRepository _locations;

    public TripSuggestionService(IFlightRepository flights, ITourRepository tours, IHotelRepository hotels, ILocationRepository locations)
    {
        _flights = flights;
        _tours = tours;
        _hotels = hotels;
        _locations = locations;
    }

    public async Task<TripSuggestionResponse> GetByHotelAsync(
        int hotelId,
        DateOnly? checkIn,
        DateOnly? checkOut,
        int guests = 1,
        CancellationToken cancellationToken = default)
    {
        var hotel = await _hotels.GetByIdAsync(hotelId, includeRooms: false, tracking: false, cancellationToken);
        if (hotel == null)
        {
            return new TripSuggestionResponse
            {
                Message = "Otel tapılmadı",
                Required = false,
                Flights = [],
                Tours = []
            };
        }

        return await GetAsync(hotel.City, hotel.Country, checkIn, checkOut, guests, cancellationToken);
    }

    public async Task<TripSuggestionResponse> GetAsync(
        string? city,
        string? country,
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
            Flights = PickFlights(flights, city, country, checkIn, checkOut, passengerCount),
            Tours = PickTours(tours, city, country, checkIn, checkOut, passengerCount)
        };
    }

    public async Task<TripSuggestionResponse> GetByCountryAsync(
        string country,
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
            Flights = flights
                .Where(flight => TravelCountry.Same(TravelCountry.OfFlight(flight), country) &&
                                 flight.SeatsAvailable >= passengerCount)
                .OrderBy(flight => flight.DepartureAt)
                .ThenBy(flight => flight.Price)
                .Select(MapFlight)
                .ToList(),
            Tours = tours
                .Where(tour => TravelCountry.Same(TravelCountry.OfTour(tour), country) &&
                               passengerCount >= tour.MinGuests && passengerCount <= tour.MaxGuests)
                .OrderBy(tour => tour.Price)
                .Select(MapTour)
                .ToList()
        };
    }

    public async Task<IReadOnlyList<TripCountryResponse>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        var flights = await _flights.GetAllAsync(cancellationToken);
        var tours = await _tours.GetAllAsync(cancellationToken);
        var withOffers = flights.Select(TravelCountry.OfFlight)
            .Concat(tours.Select(TravelCountry.OfTour))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return (await _locations.GetCountriesAsync(cancellationToken))
            .Where(country => withOffers.Contains(country.Name))
            .Select(country => new TripCountryResponse { Name = country.Name, IsoCode = country.IsoCode })
            .ToList();
    }

    private static IReadOnlyList<FlightResponse> PickFlights(
        IReadOnlyList<Flight> flights,
        string? city,
        string? country,
        DateOnly? checkIn,
        DateOnly? checkOut,
        int guests)
    {
        var open = flights.Where(flight => flight.SeatsAvailable >= guests).ToList();
        var inbound = open
            .Where(flight => (MatchesPlace(city, flight.DestinationCity, flight.DestinationCode) ||
                             MatchesPlace(country, flight.DestinationCountry)) &&
                             (!checkIn.HasValue || DateOnly.FromDateTime(flight.DepartureAt) == checkIn.Value))
            .ToList();
        var outbound = open
            .Where(flight => (MatchesPlace(city, flight.OriginCity, flight.OriginCode) ||
                             MatchesPlace(country, flight.OriginCountry)) &&
                             (!checkOut.HasValue || DateOnly.FromDateTime(flight.DepartureAt) == checkOut.Value))
            .ToList();
        var nearby = open
            .Where(flight => MatchesPlace(city, flight.DestinationCity, flight.DestinationCode) ||
                             MatchesPlace(city, flight.OriginCity, flight.OriginCode) ||
                             MatchesPlace(country, flight.DestinationCountry) ||
                             MatchesPlace(country, flight.OriginCountry))
            .ToList();

        return inbound
            .Concat(outbound)
            .Concat(nearby)
            .Concat(open.OrderBy(flight => flight.Price))
            .DistinctBy(flight => flight.FlightNumber)
            .Take(3)
            .Select(MapFlight)
            .ToList();
    }

    private static IReadOnlyList<TourResponse> PickTours(
        IReadOnlyList<Tour> tours,
        string? city,
        string? country,
        DateOnly? checkIn,
        DateOnly? checkOut,
        int guests)
    {
        var matching = tours
            .Where(tour => guests >= tour.MinGuests && guests <= tour.MaxGuests)
            .Where(tour => (MatchesPlace(city, tour.Destination, tour.Name) ||
                           MatchesPlace(country, tour.Destination)) &&
                           FitsStay(tour, checkIn, checkOut))
            .ToList();
        var fallback = tours
            .Where(tour => guests >= tour.MinGuests && guests <= tour.MaxGuests)
            .ToList();

        return matching
            .Concat(fallback)
            .DistinctBy(tour => tour.Id)
            .Take(3)
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
