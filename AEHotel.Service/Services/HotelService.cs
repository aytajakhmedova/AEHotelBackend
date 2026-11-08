using AEHotel.Service.Services.Interfaces;
using AEHotel.Service.DTOs.Hotels;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Helpers;

namespace AEHotel.Service.Services;

public class HotelService : IHotelService
{
    private readonly IHotelRepository _hotels;
    private readonly IBookingRepository _bookings;
    private readonly IReservationRepository _reservations;
    private readonly ILocationRepository _locations;

    public HotelService(IHotelRepository hotels, IBookingRepository bookings, IReservationRepository reservations, ILocationRepository locations)
    {
        _hotels = hotels;
        _bookings = bookings;
        _reservations = reservations;
        _locations = locations;
    }

    public async Task<HotelFilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
    {
        var hotels = await _hotels.GetAllAsync(cancellationToken);
        return new HotelFilterOptions
        {
            HotelTypes = hotels.Select(h => (h.HotelType ?? string.Empty).Trim())
                .Where(v => v.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v, StringComparer.CurrentCultureIgnoreCase).ToList(),
            Amenities = hotels.SelectMany(h => JsonLists.ReadStrings(h.AmenitiesJson))
                .Select(v => v.Trim()).Where(v => v.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v, StringComparer.CurrentCultureIgnoreCase).ToList(),
        };
    }

    public async Task<IReadOnlyList<HotelResponse>> GetAllAsync(string? city = null, DateOnly? checkIn = null, DateOnly? checkOut = null, int? guests = null, CancellationToken cancellationToken = default, int? adults = null, int? children = null, int? rooms = null, string? country = null, decimal? minPrice = null, decimal? maxPrice = null, string? currency = null, string? stars = null, string? hotelTypes = null, string? amenities = null)
    {
        var hotels = await _hotels.GetAllAsync(cancellationToken);
        // Hotels are priced in AZN, so a range given in another currency is converted first.
        var toAzn = ToAznRate(currency);
        var minAzn = minPrice * toAzn;
        var maxAzn = maxPrice * toAzn;
        var busyRooms = await GetBusyRoomsAsync(checkIn, checkOut, cancellationToken);
        var party = SearchParty(adults, children, rooms, guests);
        var searching = checkIn.HasValue || rooms.HasValue || adults.HasValue || guests.HasValue;
        var starSet = SplitList(stars).Select(s => int.TryParse(s, out var n) ? n : -1).ToHashSet();
        var typeSet = SplitList(hotelTypes).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var amenitySet = SplitList(amenities).ToList();
        var results = hotels
            .Where(hotel => MatchesCity(hotel, city))
            .Where(hotel => string.IsNullOrWhiteSpace(country) || string.Equals(hotel.Country, country.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(hotel => (!minAzn.HasValue || hotel.PricePerNight >= minAzn.Value)
                         && (!maxAzn.HasValue || hotel.PricePerNight <= maxAzn.Value))
            .Where(hotel => starSet.Count == 0 || starSet.Contains(hotel.Stars))
            .Where(hotel => typeSet.Count == 0 || typeSet.Contains(hotel.HotelType ?? string.Empty))
            .Where(hotel => amenitySet.Count == 0 || amenitySet.All(a => JsonLists.ReadStrings(hotel.AmenitiesJson).Contains(a, StringComparer.OrdinalIgnoreCase)))
            .Select(hotel => MapStay(hotel, busyRooms, party, searching))
            .Where(hotel => !searching || hotel.Rooms.Count > 0)
            .ToList();

        return results;
    }

    public async Task<HotelResponse> GetByIdAsync(int id, DateOnly? checkIn = null, DateOnly? checkOut = null, int? guests = null, CancellationToken cancellationToken = default, int? adults = null, int? children = null, int? rooms = null)
    {
        var hotel = await _hotels.GetByIdAsync(id, includeRooms: true, tracking: false, cancellationToken)
            ?? throw new NotFoundException($"Hotel with id {id} was not found.");

        var busyRooms = await GetBusyRoomsAsync(checkIn, checkOut, cancellationToken);
        if (hotel.IsDeleted) throw new NotFoundException("Hotel was not found.");
        return MapStay(hotel, busyRooms, SearchParty(adults, children, rooms, guests),
            checkIn.HasValue || rooms.HasValue || adults.HasValue || guests.HasValue);
    }

    public async Task<HotelResponse> CreateAsync(HotelRequest request, CancellationToken cancellationToken = default)
    {
        var location = await ValidateAsync(request, cancellationToken);

        var hotel = new Hotel();
        Apply(hotel, request, location);
        await _hotels.AddAsync(hotel, cancellationToken);
        await _hotels.SaveChangesAsync(cancellationToken);

        var response = EntityMapper.MapHotel(hotel);
        response.CountryId = location.City.CountryId;
        return response;
    }

    public async Task<HotelResponse> UpdateAsync(int id, HotelRequest request, CancellationToken cancellationToken = default)
    {
        var location = await ValidateAsync(request, cancellationToken);

        var hotel = await _hotels.GetByIdAsync(id, includeRooms: false, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Hotel with id {id} was not found.");

        Apply(hotel, request, location);
        await _hotels.SaveChangesAsync(cancellationToken);
        await _hotels.LoadRoomsAsync(hotel, cancellationToken);

        var response = EntityMapper.MapHotel(hotel);
        response.CountryId = location.City.CountryId;
        return response;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var hotel = await _hotels.GetByIdAsync(id, includeRooms: false, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Hotel with id {id} was not found.");

        // Check if already soft deleted
        if (hotel.IsDeleted)
        {
            throw new ValidationException("This hotel has already been deleted.");
        }

        // SOFT DELETE - Mark as deleted without removing from database
        hotel.IsDeleted = true;
        
        await _hotels.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<string> SplitList(string? value) =>
        (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // A search never asks for more rooms than there are guests: the extra rooms are simply dropped.
    private static StayParty SearchParty(int? adults, int? children, int? rooms, int? guests)
    {
        var party = StayParty.From(adults, children, null, guests);
        return rooms.HasValue ? party with { Rooms = Math.Clamp(rooms.Value, 1, Math.Max(1, party.Guests)) } : party;
    }

    private static HotelResponse MapStay(Hotel hotel, IReadOnlySet<int>? busy, StayParty party, bool searching)
    {
        var result = EntityMapper.MapHotel(hotel, busy);
        if (!searching) return result;
        // A single-guest search only matches genuine single rooms, not larger rooms that could also fit one person.
        var open = result.Rooms.Where(r => r.IsAvailable && r.Capacity > 0 && (party.Guests != 1 || r.Capacity == 1))
            .OrderByDescending(r => r.Capacity).ThenBy(r => r.Id).ToList();
        var largest = open.Take(party.Rooms).ToList();
        long capacity = largest.Sum(r => (long)r.Capacity);
        if (largest.Count < party.Rooms || capacity < party.Guests)
            result.Rooms = [];
        else
            // Rooms that can be part of a fitting set, the snuggest fit first (a 1-guest search lists
            // single rooms before the big family ones), cheapest first among equals.
            result.Rooms = open.Where(r => capacity - largest[^1].Capacity + r.Capacity >= party.Guests)
                .OrderBy(r => r.Capacity).ThenBy(r => r.PricePerNight).ThenBy(r => r.Id).ToList();
        return result;
    }

    // Approximate fixed rates, the same ones the site uses to display prices (1 USD = 1.7 AZN, 1 EUR = 1.85 AZN).
    // TRY is derived from the AZN and TRY seasonal price tables shown on the home page.
    private static decimal ToAznRate(string? currency) => currency?.Trim().ToUpperInvariant() switch
    {
        "EUR" => 1.85m,
        "USD" => 1.7m,
        "TRY" => 0.065m,
        _ => 1m,
    };

    private static bool MatchesCity(Hotel hotel, string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return true;
        }

        var value = city.Trim();
        return hotel.City.Contains(value, StringComparison.OrdinalIgnoreCase) ||
               hotel.Country.Contains(value, StringComparison.OrdinalIgnoreCase) ||
               hotel.Location.Contains(value, StringComparison.OrdinalIgnoreCase) ||
               hotel.Title.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    private static void Validate(HotelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ValidationException("Hotel title is required.");
        }

        if (request.Stars is < 1 or > 5)
        {
            throw new ValidationException("Stars must be between 1 and 5.");
        }

        if (request.PricePerNight < 0)
        {
            throw new ValidationException("Price per night cannot be negative.");
        }
    }

    private async Task<(City City, TouristPlace? Place)> ValidateAsync(HotelRequest request, CancellationToken cancellationToken)
    {
        Validate(request);

        if (request.CountryId is not > 0)
        {
            throw new ValidationException("Country is required.");
        }

        if (request.CityId is not > 0)
        {
            throw new ValidationException("City is required.");
        }

        var country = await _locations.GetCountryAsync(request.CountryId.Value, cancellationToken)
            ?? throw new ValidationException($"Country with id {request.CountryId} does not exist.");

        var city = await _locations.GetCityAsync(request.CityId.Value, cancellationToken)
            ?? throw new ValidationException($"City with id {request.CityId} does not exist.");
        if (city.CountryId != country.Id)
        {
            throw new ValidationException($"City '{city.Name}' does not belong to country '{country.Name}'.");
        }

        // The tourist place is optional: without it the hotel is simply placed in the city.
        TouristPlace? place = null;
        if (request.TouristPlaceId is > 0)
        {
            place = await _locations.GetTouristPlaceAsync(request.TouristPlaceId.Value, cancellationToken)
                ?? throw new ValidationException($"Tourist place with id {request.TouristPlaceId} does not exist.");
        }

        if (place != null && place.CityId != city.Id)
        {
            throw new ValidationException($"Tourist place '{place.Name}' does not belong to city '{city.Name}'.");
        }

        return (city, place);
    }

    private static void Apply(Hotel hotel, HotelRequest request, (City City, TouristPlace? Place) selected)
    {
        hotel.Title = request.Title.Trim();
        hotel.CityId = selected.City.Id;
        hotel.TouristPlaceId = selected.Place?.Id;
        // readable copies of the selected names, kept for search and existing clients
        hotel.City = selected.City.Name;
        hotel.Country = selected.City.Country?.Name ?? string.Empty;
        hotel.Location = selected.Place?.Name ?? selected.City.Name;
        hotel.Address = request.Address.Trim();
        hotel.Stars = request.Stars;
        hotel.Description = request.Description.Trim();
        hotel.PricePerNight = request.PricePerNight;
        hotel.HotelType = request.HotelType.Trim();
        hotel.CheckInTime = string.IsNullOrWhiteSpace(request.CheckInTime) ? "14:00" : request.CheckInTime.Trim();
        hotel.CheckOutTime = string.IsNullOrWhiteSpace(request.CheckOutTime) ? "12:00" : request.CheckOutTime.Trim();
        hotel.CancellationPolicy = string.IsNullOrWhiteSpace(request.CancellationPolicy)
            ? "Check-in tarixindən əvvəl ləğv etmək olar."
            : request.CancellationPolicy.Trim();
        hotel.ImageFile = request.ImageFile.Trim();
        if (request.GalleryImages is not null)
        {
            var gallery = request.GalleryImages
                .Select(file => file.Trim())
                .Where(file => file.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3);
            hotel.GalleryJson = JsonLists.WriteStrings(gallery);
        }
        hotel.AmenitiesJson = JsonLists.WriteStrings(request.Amenities);
    }

    private async Task<IReadOnlySet<int>?> GetBusyRoomsAsync(DateOnly? checkIn, DateOnly? checkOut, CancellationToken cancellationToken)
    {
        // Only a check-in date means a one-night stay; a lone check-out date says nothing about availability.
        if (checkIn.HasValue && !checkOut.HasValue)
        {
            checkOut = checkIn.Value.AddDays(1);
        }

        if (!checkIn.HasValue || !checkOut.HasValue)
        {
            return null;
        }

        if (checkOut <= checkIn)
            throw new ValidationException("Check-out must be after check-in.");

        var busyBookings = await _bookings.GetBusyRoomIdsAsync(checkIn.Value, checkOut.Value, cancellationToken);
        var busyReservations = await _reservations.GetBusyRoomIdsAsync(checkIn.Value, checkOut.Value, cancellationToken);
        return busyBookings.Concat(busyReservations).ToHashSet();
    }
}
