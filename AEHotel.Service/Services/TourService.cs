using AEHotel.Service.Services.Interfaces;
using AEHotel.Service.DTOs.Tours;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Helpers;

namespace AEHotel.Service.Services;

public class TourService : ITourService
{
    private readonly ITourRepository _tours;
    private readonly IBookingRepository _bookings;
    private readonly IHotelRepository _hotels;
    private readonly ITourCategoryService _categories;

    public TourService(ITourRepository tours, IBookingRepository bookings, IHotelRepository hotels, ITourCategoryService categories)
    {
        _hotels = hotels;
        _categories = categories;
        _tours = tours;
        _bookings = bookings;
    }

    public async Task<IReadOnlyList<TourResponse>> GetAllAsync(string? destination = null, DateOnly? date = null, int? guests = null, CancellationToken cancellationToken = default, string? category = null, string? sort = null, int? hotelId = null, bool includePast = false)
    {
        var tours = await _tours.GetAllAsync(cancellationToken);
        var results = new List<TourResponse>();

        foreach (var tour in tours)
        {
            if (!MatchesDestination(tour, destination) || !MatchesGuests(tour, guests) || !MatchesCategory(tour, category) || (hotelId.HasValue && tour.HotelId != hotelId.Value))
            {
                continue;
            }

            // Guests do not see a tour whose dates have all passed, nor its past dates; staff see everything.
            if (!includePast && !TourDates.HasUpcoming(tour))
            {
                continue;
            }

            var mapped = Map(tour);
            if (!includePast)
            {
                mapped.AvailableDates = TourDates.Upcoming(mapped.AvailableDates);
            }

            // A tour only runs on fixed dates, so a date that is not one of them must not hide the
            // tour from the search; only a date the tour actually runs on can leave it without places.
            if (date.HasValue && MatchesDate(tour, date))
            {
                var reserved = await _bookings.CountTourGuestsAsync(tour.Id, date.Value, excludeBookingId: null, cancellationToken);
                mapped.RemainingPlaces = Math.Max(0, tour.MaxGuests - reserved);
                if (guests.HasValue && mapped.RemainingPlaces < guests.Value)
                {
                    continue;
                }
            }

            results.Add(mapped);
        }

        return Sort(results, sort);
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var tours = await _tours.GetAllAsync(cancellationToken);
        return tours.Select(t => t.Category).Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool MatchesCategory(Tour tour, string? category) =>
        string.IsNullOrWhiteSpace(category) || string.Equals(tour.Category, category.Trim(), StringComparison.OrdinalIgnoreCase);

    private static List<TourResponse> Sort(List<TourResponse> tours, string? sort) => sort?.Trim().ToLowerInvariant() switch
    {
        "price-low" => tours.OrderBy(t => t.Price).ThenBy(t => t.Id).ToList(),
        "price-high" => tours.OrderByDescending(t => t.Price).ThenBy(t => t.Id).ToList(),
        "rating" => tours.OrderByDescending(t => t.Rating).ThenBy(t => t.Id).ToList(),
        "duration" => tours.OrderBy(t => t.Duration, StringComparer.OrdinalIgnoreCase).ThenBy(t => t.Id).ToList(),
        _ => tours.OrderByDescending(t => t.ReviewCount).ThenBy(t => t.Id).ToList(),
    };

    public async Task<TourResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default, bool includePast = false)
    {
        var tour = await _tours.GetByIdAsync(id, tracking: false, cancellationToken)
            ?? throw new NotFoundException($"Tour with id {id} was not found.");
        if (!includePast && !TourDates.HasUpcoming(tour))
        {
            throw new NotFoundException($"Tour with id {id} was not found.");
        }

        var response = Map(tour);
        if (!includePast)
        {
            response.AvailableDates = TourDates.Upcoming(response.AvailableDates);
        }

        foreach (var date in response.AvailableDates)
        {
            if (!DateOnly.TryParse(date, out var day)) continue;
            var reserved = await _bookings.CountTourGuestsAsync(tour.Id, day, excludeBookingId: null, cancellationToken);
            response.Availability.Add(new TourDateAvailability { Date = date, RemainingPlaces = Math.Max(0, tour.MaxGuests - reserved) });
        }

        return response;
    }

    public async Task<TourResponse> CreateAsync(TourRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var hotel = await RequireHotelAsync(request, cancellationToken);
        await RequireCategoryAsync(request, cancellationToken);
        var tour = new Tour();
        Apply(tour, request);
        tour.Hotel = hotel;
        // The destination is always the hotel's city and country, so the country is read correctly everywhere.
        tour.Destination = string.Join(", ", new[] { hotel.City, hotel.Country }.Where(part => !string.IsNullOrWhiteSpace(part)));
        await _tours.AddAsync(tour, cancellationToken);
        await _tours.SaveChangesAsync(cancellationToken);
        return Map(tour);
    }

    public async Task<TourResponse> UpdateAsync(int id, TourRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var hotel = await RequireHotelAsync(request, cancellationToken);
        await RequireCategoryAsync(request, cancellationToken);
        var tour = await _tours.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Tour with id {id} was not found.");
        Apply(tour, request);
        tour.Hotel = hotel;
        // The destination is always the hotel's city and country, so the country is read correctly everywhere.
        tour.Destination = string.Join(", ", new[] { hotel.City, hotel.Country }.Where(part => !string.IsNullOrWhiteSpace(part)));
        await _tours.SaveChangesAsync(cancellationToken);
        return Map(tour);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var tour = await _tours.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Tour with id {id} was not found.");

        if (await _bookings.ExistsForTourAsync(id, cancellationToken))
        {
            throw new ValidationException("This tour cannot be deleted because it has reservations.");
        }

        _tours.Remove(tour);
        await _tours.SaveChangesAsync(cancellationToken);
    }

    private static bool MatchesDestination(Tour tour, string? destination)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            return true;
        }

        var value = destination.Trim();
        return tour.Destination.Contains(value, StringComparison.OrdinalIgnoreCase) ||
               tour.Name.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesGuests(Tour tour, int? guests)
    {
        return !guests.HasValue || (guests.Value >= tour.MinGuests && guests.Value <= tour.MaxGuests);
    }

    private static bool MatchesDate(Tour tour, DateOnly? date)
    {
        if (!date.HasValue)
        {
            return true;
        }

        var dates = JsonLists.ReadStrings(tour.AvailableDatesJson);
        return dates.Count == 0 || dates.Contains(date.Value.ToString("yyyy-MM-dd"));
    }

    // The category has to be one created by an admin; its stored spelling is used.
    private async Task RequireCategoryAsync(TourRequest request, CancellationToken cancellationToken)
    {
        request.Category = await _categories.FindNameAsync(request.Category, cancellationToken)
            ?? throw new ValidationException("Please select a tour category from the list.");
    }

    // A tour always belongs to an existing hotel; guests booking that hotel are offered the tour.
    private async Task<Hotel> RequireHotelAsync(TourRequest request, CancellationToken cancellationToken)
    {
        if (!request.HotelId.HasValue)
        {
            throw new ValidationException("Please select the hotel this tour belongs to.");
        }

        var hotel = await _hotels.GetByIdAsync(request.HotelId.Value, includeRooms: false, tracking: false, cancellationToken);
        if (hotel == null || hotel.IsDeleted)
        {
            throw new ValidationException("The selected hotel does not exist.");
        }

        return hotel;
    }

    private static readonly string[] Difficulties = ["Asan", "Orta", "Çətin"];

    private static void Validate(TourRequest request)
    {
        var difficulty = Difficulties.FirstOrDefault(item => string.Equals(item, (request.Difficulty ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase));
        if (difficulty == null)
        {
            throw new ValidationException("Difficulty must be Asan, Orta or Çətin.");
        }

        request.Difficulty = difficulty;

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Tour name is required.");
        }

        if (request.Price < 0)
        {
            throw new ValidationException("Price cannot be negative.");
        }

        if (request.MinGuests < 1 || request.MaxGuests < request.MinGuests)
        {
            throw new ValidationException("Guest limits are invalid.");
        }
    }

    private static void Apply(Tour tour, TourRequest request)
    {
        tour.Name = request.Name.Trim();
        tour.Destination = request.Destination.Trim();
        tour.Rating = request.Rating;
        tour.ReviewCount = request.ReviewCount;
        tour.Duration = request.Duration.Trim();
        tour.Price = request.Price;
        tour.ImageFile = request.ImageFile.Trim();
        tour.Description = request.Description.Trim();
        tour.Category = request.Category.Trim();
        tour.Difficulty = request.Difficulty.Trim();
        tour.MaxGuests = request.MaxGuests;
        tour.MinGuests = request.MinGuests;
        tour.HighlightsJson = JsonLists.WriteStrings(request.Highlights);
        tour.IncludedJson = JsonLists.WriteStrings(request.Included);
        tour.ExcludedJson = JsonLists.WriteStrings(request.Excluded);
        tour.ItineraryJson = JsonLists.Write(request.Itinerary);
        tour.AvailableDatesJson = JsonLists.WriteStrings(request.AvailableDates);
        tour.HotelId = request.HotelId;
    }

    private static TourResponse Map(Tour tour)
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
            AvailableDates = JsonLists.ReadStrings(tour.AvailableDatesJson),
            HotelId = tour.HotelId,
            HotelName = tour.Hotel?.Title ?? string.Empty
        };
    }
}
