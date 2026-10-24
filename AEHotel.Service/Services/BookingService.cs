using AEHotel.Service.Services.Interfaces;
using AEHotel.Service.DTOs.Bookings;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Helpers;

namespace AEHotel.Service.Services;

public class BookingService : IBookingService
{
    private static readonly Dictionary<string, string[]> NextStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        ["confirmed"] = ["checked-in", "cancelled"],
        ["checked-in"] = ["checked-out"],
        ["checked-out"] = [],
        ["cancelled"] = []
    };

    private readonly IBookingRepository _bookings;
    private readonly IHotelRepository _hotels;
    private readonly IRoomRepository _rooms;
    private readonly IReservationRepository _reservations;
    private readonly IFlightRepository _flights;
    private readonly ITourRepository _tours;
    private readonly ITripSuggestionService _suggestions;

    public BookingService(
        IBookingRepository bookings,
        IHotelRepository hotels,
        IRoomRepository rooms,
        IReservationRepository reservations,
        IFlightRepository flights,
        ITourRepository tours,
        ITripSuggestionService suggestions)
    {
        _bookings = bookings;
        _hotels = hotels;
        _rooms = rooms;
        _reservations = reservations;
        _flights = flights;
        _tours = tours;
        _suggestions = suggestions;
    }

    public async Task<IReadOnlyList<BookingResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var bookings = await _bookings.GetAllAsync(cancellationToken);
        return bookings.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<BookingResponse>> GetMineAsync(string userId, string? bookingType = null, CancellationToken cancellationToken = default)
    {
        var bookings = await _bookings.GetByUserIdAsync(userId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(bookingType))
        {
            var type = bookingType.Trim().ToLowerInvariant();
            bookings = bookings.Where(booking => booking.BookingType == type).ToList();
        }

        return bookings.Select(Map).ToList();
    }

    public async Task<BookingResponse> GetByIdAsync(int id, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var booking = await _bookings.GetByIdAsync(id, tracking: false, cancellationToken)
            ?? throw new NotFoundException($"Booking with id {id} was not found.");
        EnsureCanAccess(booking, userId, isAdmin);
        return await MapWithSuggestionsAsync(booking, cancellationToken);
    }

    public async Task<BookingResponse> CreateAsync(BookingRequest request, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedException("Authentication is required.");
        }

        ValidateGuest(request);
        var bookingType = NormalizeType(request.BookingType);
        var booking = new Booking
        {
            Code = CreateCode(),
            BookingType = bookingType,
            Status = "confirmed",
            UserId = userId,
            GuestName = request.GuestName.Trim(),
            GuestEmail = request.GuestEmail.Trim(),
            GuestPhone = request.GuestPhone?.Trim() ?? string.Empty,
            SpecialRequests = request.SpecialRequests?.Trim() ?? string.Empty,
            Guests = request.Guests,
            CreatedAt = DateTime.UtcNow
        };

        if (bookingType == "hotel")
        {
            await ApplyHotelAsync(booking, request, excludeBookingId: null, cancellationToken);
        }
        else if (bookingType == "flight")
        {
            await ApplyFlightAsync(booking, request, cancellationToken);
        }
        else
        {
            await ApplyTourAsync(booking, request, excludeBookingId: null, cancellationToken);
        }

        booking.Payment = CreatePayment(request, booking.TotalPrice);
        await _bookings.AddAsync(booking, cancellationToken);
        await _bookings.SaveChangesAsync(cancellationToken);

        var saved = await _bookings.GetByIdAsync(booking.Id, tracking: false, cancellationToken);
        return await MapWithSuggestionsAsync(saved!, cancellationToken);
    }

    public async Task<BookingResponse> UpdateAsync(int id, BookingUpdateRequest request, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var booking = await _bookings.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Booking with id {id} was not found.");
        EnsureCanAccess(booking, userId, isAdmin);

        if (!string.Equals(booking.Status, "confirmed", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Only confirmed bookings can be changed.");
        }

        if (booking.BookingType != "hotel")
        {
            throw new ValidationException("Only hotel bookings can change dates from this action.");
        }

        var hotelRequest = new BookingRequest
        {
            HotelId = booking.HotelId,
            RoomId = booking.RoomId,
            GuestName = booking.GuestName,
            GuestEmail = booking.GuestEmail,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            Guests = request.Guests,
            SpecialRequests = request.SpecialRequests
        };

        await ApplyHotelAsync(booking, hotelRequest, id, cancellationToken);
        if (request.SpecialRequests != null)
        {
            booking.SpecialRequests = request.SpecialRequests.Trim();
        }

        if (booking.Payment != null)
        {
            booking.Payment.Amount = booking.TotalPrice;
        }

        await _bookings.SaveChangesAsync(cancellationToken);
        var saved = await _bookings.GetByIdAsync(id, tracking: false, cancellationToken);
        return Map(saved!);
    }

    public async Task<BookingResponse> UpdateStatusAsync(int id, string status, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var booking = await _bookings.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Booking with id {id} was not found.");
        EnsureCanAccess(booking, userId, isAdmin);

        var next = status.Trim().ToLowerInvariant();
        if (!NextStatuses.TryGetValue(booking.Status, out var allowed) || !allowed.Contains(next, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException($"Status cannot change from {booking.Status} to {next}.");
        }

        if (next == "cancelled")
        {
            await ReleaseInventoryAsync(booking, cancellationToken);
            if (booking.Payment != null && booking.Payment.Status == "paid")
            {
                booking.Payment.Status = "refunded";
            }
            else if (booking.Payment != null)
            {
                booking.Payment.Status = "cancelled";
            }
        }

        booking.Status = next;
        await _bookings.SaveChangesAsync(cancellationToken);
        var saved = await _bookings.GetByIdAsync(id, tracking: false, cancellationToken);
        return Map(saved!);
    }

    public async Task DeleteAsync(int id, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        await UpdateStatusAsync(id, "cancelled", userId, isAdmin, cancellationToken);
    }

    private static void EnsureCanAccess(Booking booking, string userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(userId) || booking.UserId != userId)
        {
            throw new NotFoundException($"Booking with id {booking.Id} was not found.");
        }
    }

    private async Task ApplyHotelAsync(Booking booking, BookingRequest request, int? excludeBookingId, CancellationToken cancellationToken)
    {
        if (!request.HotelId.HasValue || !request.RoomId.HasValue || !request.CheckIn.HasValue || !request.CheckOut.HasValue)
        {
            throw new ValidationException("Hotel, room, and stay dates are required.");
        }

        if (request.CheckOut <= request.CheckIn)
        {
            throw new ValidationException("Check-out date must be after the check-in date.");
        }

        if (request.CheckIn.Value < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ValidationException("Check-in date cannot be in the past.");
        }

        if (!await _hotels.ExistsAsync(request.HotelId.Value, cancellationToken))
        {
            throw new NotFoundException($"Hotel with id {request.HotelId} was not found.");
        }

        var room = await _rooms.GetByIdAsync(request.RoomId.Value, tracking: false, cancellationToken)
            ?? throw new NotFoundException($"Room with id {request.RoomId} was not found.");

        if (room.HotelId != request.HotelId)
        {
            throw new ValidationException("The selected room does not belong to the selected hotel.");
        }

        if (request.Guests > room.Capacity)
        {
            throw new ValidationException($"This room allows at most {room.Capacity} guests.");
        }

        if (!room.Available)
        {
            throw new ValidationException("This room is not available for booking.");
        }

        if (await _bookings.HasHotelOverlapAsync(room.Id, request.CheckIn.Value, request.CheckOut.Value, excludeBookingId, cancellationToken) ||
            await _reservations.HasOverlapAsync(room.Id, request.CheckIn.Value, request.CheckOut.Value, excludeReservationId: null, cancellationToken))
        {
            throw new ValidationException("The room is already reserved for the selected dates.");
        }

        var price = StayPriceCalculator.Calculate(room.PricePerNight, request.CheckIn.Value, request.CheckOut.Value, request.Guests);
        var hotelTitle = await _hotels.GetTitleAsync(room.HotelId, cancellationToken) ?? string.Empty;
        booking.HotelId = room.HotelId;
        booking.RoomId = room.Id;
        booking.CheckIn = request.CheckIn;
        booking.CheckOut = request.CheckOut;
        booking.Guests = request.Guests;
        booking.Nights = price.Nights;
        booking.PricePerNight = room.PricePerNight;
        booking.TotalPrice = price.Total;
        booking.ItemName = room.Name;
        booking.Location = hotelTitle;
    }

    private async Task ApplyFlightAsync(Booking booking, BookingRequest request, CancellationToken cancellationToken)
    {
        if (!request.FlightId.HasValue)
        {
            throw new ValidationException("Flight is required.");
        }

        var flight = await _flights.GetByIdAsync(request.FlightId.Value, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Flight with id {request.FlightId} was not found.");

        if (flight.SeatsAvailable < request.Guests)
        {
            throw new ValidationException("Not enough seats are left on this flight.");
        }

        flight.SeatsAvailable -= request.Guests;
        booking.FlightId = flight.Id;
        booking.CheckIn = DateOnly.FromDateTime(flight.DepartureAt);
        booking.Guests = request.Guests;
        booking.Nights = 0;
        booking.PricePerNight = flight.Price;
        booking.TotalPrice = ApplyBundlePrice(flight.Price * request.Guests, request.BundleWithHotel);
        booking.ItemName = $"{flight.Airline} {flight.FlightNumber}";
        booking.Location = $"{flight.OriginCity} ({flight.OriginCode}) - {flight.DestinationCity} ({flight.DestinationCode})";
    }

    private async Task ApplyTourAsync(Booking booking, BookingRequest request, int? excludeBookingId, CancellationToken cancellationToken)
    {
        if (!request.TourId.HasValue || !request.TravelDate.HasValue)
        {
            throw new ValidationException("Tour and travel date are required.");
        }

        var tour = await _tours.GetByIdAsync(request.TourId.Value, tracking: false, cancellationToken)
            ?? throw new NotFoundException($"Tour with id {request.TourId} was not found.");

        if (request.Guests < tour.MinGuests || request.Guests > tour.MaxGuests)
        {
            throw new ValidationException($"This tour accepts {tour.MinGuests} to {tour.MaxGuests} guests.");
        }

        var dates = JsonLists.ReadStrings(tour.AvailableDatesJson);
        if (dates.Count > 0 && !dates.Contains(request.TravelDate.Value.ToString("yyyy-MM-dd")))
        {
            throw new ValidationException("The selected date is not available for this tour.");
        }

        var reserved = await _bookings.CountTourGuestsAsync(tour.Id, request.TravelDate.Value, excludeBookingId, cancellationToken);
        if (reserved + request.Guests > tour.MaxGuests)
        {
            throw new ValidationException("This tour date does not have enough remaining places.");
        }

        booking.TourId = tour.Id;
        booking.CheckIn = request.TravelDate;
        booking.Guests = request.Guests;
        booking.Nights = 0;
        booking.PricePerNight = tour.Price;
        booking.TotalPrice = ApplyBundlePrice(tour.Price * request.Guests, request.BundleWithHotel);
        booking.ItemName = tour.Name;
        booking.Location = tour.Destination;
    }

    private async Task ReleaseInventoryAsync(Booking booking, CancellationToken cancellationToken)
    {
        if (booking.BookingType == "flight" && booking.FlightId.HasValue)
        {
            var flight = await _flights.GetByIdAsync(booking.FlightId.Value, tracking: true, cancellationToken);
            if (flight != null)
            {
                flight.SeatsAvailable += booking.Guests;
            }
        }
    }

    private static Payment CreatePayment(BookingRequest request, decimal amount)
    {
        var method = NormalizePaymentMethod(request.PaymentMethod);
        var isCard = method == "card";
        var digits = isCard
            ? new string((request.CardNumber ?? string.Empty).Where(char.IsDigit).ToArray())
            : string.Empty;

        return new Payment
        {
            Method = method,
            Amount = amount,
            Status = "pending",
            CardLast4 = digits.Length >= 4 ? digits[^4..] : string.Empty,
            CardHolder = isCard ? request.CardHolder?.Trim() ?? string.Empty : string.Empty,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string NormalizePaymentMethod(string? paymentMethod)
    {
        var method = string.IsNullOrWhiteSpace(paymentMethod)
            ? "card"
            : paymentMethod.Trim().ToLowerInvariant();

        if (method is not ("card" or "cash" or "hotel"))
        {
            throw new ValidationException("Payment method must be card or cash.");
        }

        return method;
    }

    private static void ValidateGuest(BookingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GuestName))
        {
            throw new ValidationException("Guest name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.GuestEmail) || !request.GuestEmail.Contains('@'))
        {
            throw new ValidationException("A valid guest email is required.");
        }

        if (request.Guests < 1)
        {
            throw new ValidationException("At least one guest is required.");
        }
    }

    private static string NormalizeType(string? bookingType)
    {
        var value = (bookingType ?? "hotel").Trim().ToLowerInvariant();
        if (value is not ("hotel" or "flight" or "tour"))
        {
            throw new ValidationException("Booking type must be hotel, flight, or tour.");
        }

        return value;
    }

    private static decimal ApplyBundlePrice(decimal total, bool bundleWithHotel)
    {
        if (!bundleWithHotel)
        {
            return total;
        }

        return Math.Round(total * 0.9m, 2, MidpointRounding.AwayFromZero);
    }

    private static string CreateCode()
    {
        return $"OB-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }

    private async Task<BookingResponse> MapWithSuggestionsAsync(Booking booking, CancellationToken cancellationToken)
    {
        var response = Map(booking);
        if (booking.BookingType != "hotel")
        {
            return response;
        }

        response.TripSuggestions = await _suggestions.GetAsync(
            booking.Hotel?.City ?? booking.Location,
            booking.CheckIn,
            booking.CheckOut,
            booking.Guests,
            cancellationToken);
        return response;
    }

    private static BookingResponse Map(Booking booking)
    {
        return new BookingResponse
        {
            Id = booking.Id,
            Code = booking.Code,
            BookingType = booking.BookingType,
            Status = booking.Status,
            HotelId = booking.HotelId,
            HotelName = booking.Hotel?.Title ?? (booking.BookingType == "hotel" ? booking.Location : string.Empty),
            RoomId = booking.RoomId,
            RoomName = booking.Room?.Name ?? booking.ItemName,
            FlightId = booking.FlightId,
            FlightNumber = booking.Flight?.FlightNumber ?? string.Empty,
            TourId = booking.TourId,
            TourName = booking.Tour?.Name ?? (booking.BookingType == "tour" ? booking.ItemName : string.Empty),
            GuestName = booking.GuestName,
            GuestEmail = booking.GuestEmail,
            GuestPhone = booking.GuestPhone,
            CheckIn = booking.CheckIn,
            CheckOut = booking.CheckOut,
            Guests = booking.Guests,
            Nights = booking.Nights,
            PricePerNight = booking.PricePerNight,
            TotalPrice = booking.TotalPrice,
            ItemName = booking.ItemName,
            Location = booking.Location,
            SpecialRequests = booking.SpecialRequests,
            PaymentMethod = booking.Payment?.Method ?? string.Empty,
            PaymentStatus = booking.Payment?.Status ?? string.Empty,
            PaymentId = booking.Payment?.Id,
            CreatedAt = booking.CreatedAt
        };
    }
}
