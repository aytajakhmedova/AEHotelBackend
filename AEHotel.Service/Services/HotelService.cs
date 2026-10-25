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

    public HotelService(IHotelRepository hotels, IBookingRepository bookings, IReservationRepository reservations)
    {
        _hotels = hotels;
        _bookings = bookings;
        _reservations = reservations;
    }

    public async Task<IReadOnlyList<HotelResponse>> GetAllAsync(string? city = null, DateOnly? checkIn = null, DateOnly? checkOut = null, int? guests = null, CancellationToken cancellationToken = default)
    {
        var hotels = await _hotels.GetAllAsync(cancellationToken);
        var busyRooms = await GetBusyRoomsAsync(checkIn, checkOut, cancellationToken);
        var results = hotels
            .Where(hotel => MatchesCity(hotel, city))
            .Select(hotel => EntityMapper.MapHotel(hotel, busyRooms, guests))
            .ToList();

        if (guests.HasValue || (checkIn.HasValue && checkOut.HasValue))
        {
            results = results.Where(hotel => hotel.Rooms.Any(room => room.IsAvailable)).ToList();
        }

        return results;
    }

    public async Task<HotelResponse> GetByIdAsync(int id, DateOnly? checkIn = null, DateOnly? checkOut = null, int? guests = null, CancellationToken cancellationToken = default)
    {
        var hotel = await _hotels.GetByIdAsync(id, includeRooms: true, tracking: false, cancellationToken)
            ?? throw new NotFoundException($"Hotel with id {id} was not found.");

        var busyRooms = await GetBusyRoomsAsync(checkIn, checkOut, cancellationToken);
        return EntityMapper.MapHotel(hotel, busyRooms, guests);
    }

    public async Task<HotelResponse> CreateAsync(HotelRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);

        var hotel = new Hotel();
        Apply(hotel, request);
        await _hotels.AddAsync(hotel, cancellationToken);
        await _hotels.SaveChangesAsync(cancellationToken);

        return EntityMapper.MapHotel(hotel);
    }

    public async Task<HotelResponse> UpdateAsync(int id, HotelRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);

        var hotel = await _hotels.GetByIdAsync(id, includeRooms: false, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Hotel with id {id} was not found.");

        Apply(hotel, request);
        await _hotels.SaveChangesAsync(cancellationToken);
        await _hotels.LoadRoomsAsync(hotel, cancellationToken);

        return EntityMapper.MapHotel(hotel);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var hotel = await _hotels.GetByIdAsync(id, includeRooms: false, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Hotel with id {id} was not found.");

        if (await _hotels.HasReservationsAsync(id, cancellationToken) ||
            await _bookings.ExistsForHotelAsync(id, cancellationToken))
        {
            throw new ValidationException("This hotel cannot be deleted because it has reservations.");
        }

        _hotels.Remove(hotel);
        await _hotels.SaveChangesAsync(cancellationToken);
    }

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

        if (string.IsNullOrWhiteSpace(request.City))
        {
            throw new ValidationException("City is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Country))
        {
            throw new ValidationException("Country is required.");
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

    private static void Apply(Hotel hotel, HotelRequest request)
    {
        hotel.Title = request.Title.Trim();
        hotel.City = request.City.Trim();
        hotel.Country = request.Country.Trim();
        hotel.Location = request.Location.Trim();
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
        hotel.AmenitiesJson = JsonLists.WriteStrings(request.Amenities);
    }

    private async Task<IReadOnlySet<int>?> GetBusyRoomsAsync(DateOnly? checkIn, DateOnly? checkOut, CancellationToken cancellationToken)
    {
        if (!checkIn.HasValue || !checkOut.HasValue || checkOut <= checkIn)
        {
            return null;
        }

        var busyBookings = await _bookings.GetBusyRoomIdsAsync(checkIn.Value, checkOut.Value, cancellationToken);
        var busyReservations = await _reservations.GetBusyRoomIdsAsync(checkIn.Value, checkOut.Value, cancellationToken);
        return busyBookings.Concat(busyReservations).ToHashSet();
    }
}
