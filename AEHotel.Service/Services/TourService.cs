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

    public TourService(ITourRepository tours, IBookingRepository bookings)
    {
        _tours = tours;
        _bookings = bookings;
    }

    public async Task<IReadOnlyList<TourResponse>> GetAllAsync(string? destination = null, DateOnly? date = null, int? guests = null, CancellationToken cancellationToken = default)
    {
        var tours = await _tours.GetAllAsync(cancellationToken);
        var results = new List<TourResponse>();

        foreach (var tour in tours)
        {
            if (!MatchesDestination(tour, destination) || !MatchesGuests(tour, guests) || !MatchesDate(tour, date))
            {
                continue;
            }

            var mapped = Map(tour);
            if (date.HasValue)
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

        return results;
    }

    public async Task<TourResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var tour = await _tours.GetByIdAsync(id, tracking: false, cancellationToken)
            ?? throw new NotFoundException($"Tour with id {id} was not found.");
        return Map(tour);
    }

    public async Task<TourResponse> CreateAsync(TourRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var tour = new Tour();
        Apply(tour, request);
        await _tours.AddAsync(tour, cancellationToken);
        await _tours.SaveChangesAsync(cancellationToken);
        return Map(tour);
    }

    public async Task<TourResponse> UpdateAsync(int id, TourRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var tour = await _tours.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Tour with id {id} was not found.");
        Apply(tour, request);
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

    private static void Validate(TourRequest request)
    {
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
            AvailableDates = JsonLists.ReadStrings(tour.AvailableDatesJson)
        };
    }
}
