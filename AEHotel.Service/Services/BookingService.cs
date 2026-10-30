using AEHotel.Service.Services.Interfaces;
using AEHotel.Service.DTOs.Bookings;
using AEHotel.Service.DTOs.Discounts;
using AEHotel.Service.DTOs.Email;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Helpers;
using Microsoft.Extensions.Logging;

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
    private readonly IDiscountService _discounts;
    private readonly IEmailSender _email;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IBookingRepository bookings,
        IHotelRepository hotels,
        IRoomRepository rooms,
        IReservationRepository reservations,
        IFlightRepository flights,
        ITourRepository tours,
        ITripSuggestionService suggestions,
        IDiscountService discounts,
        IEmailSender email,
        ILogger<BookingService> logger)
    {
        _bookings = bookings;
        _hotels = hotels;
        _rooms = rooms;
        _reservations = reservations;
        _flights = flights;
        _tours = tours;
        _suggestions = suggestions;
        _discounts = discounts;
        _email = email;
        _logger = logger;
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
        var response = await MapWithSuggestionsAsync(saved!, cancellationToken);
        response.EmailSent = await TrySendConfirmationAsync(response, cancellationToken);
        return response;
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
            var percent = booking.Payment.DepositPercent;
            if (percent <= 0 && booking.Payment.Method == "card")
            {
                percent = 100;
            }

            if (percent > 0)
            {
                ApplyDeposit(booking.Payment, booking.TotalPrice, percent);
            }
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
            if (!isAdmin &&
                booking.CheckIn.HasValue &&
                booking.CheckIn.Value <= DateOnly.FromDateTime(DateTime.UtcNow.Date))
            {
                throw new ValidationException("Check-in günü və ya sonra rezervasiyanı ləğv etmək olmaz.");
            }

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

    public async Task<BookingResponse> SendReceiptEmailAsync(int id, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var response = await GetByIdAsync(id, userId, isAdmin, cancellationToken);
        if (string.IsNullOrWhiteSpace(response.GuestEmail) || !response.GuestEmail.Contains('@'))
        {
            throw new ValidationException("Rezervasiyada real email olmalıdır.");
        }

        if (!_email.DeliversToInbox)
        {
            throw new ValidationException("Gmail SMTP şifrəsi yüklənməyib.");
        }

        response.EmailSent = await TrySendConfirmationAsync(response, cancellationToken, throwOnFailure: true);
        return response;
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
        await ApplyPricingAsync(booking, price.Total, request, applyAmount: true, cancellationToken);
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
        await ApplyPricingAsync(booking, flight.Price * request.Guests, request, applyAmount: true, cancellationToken);
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
        await ApplyPricingAsync(booking, tour.Price * request.Guests, request, applyAmount: true, cancellationToken);
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

    private static readonly int[] HotelDepositPercents = [20, 30, 40];

    private static Payment CreatePayment(BookingRequest request, decimal amount)
    {
        var method = NormalizePaymentMethod(request.PaymentMethod);
        var needsCard = method is "card" or "hotel";
        var digits = needsCard
            ? new string((request.CardNumber ?? string.Empty).Where(char.IsDigit).ToArray())
            : string.Empty;

        if (needsCard)
        {
            if (digits.Length < 13)
            {
                throw new ValidationException("Kart nömrəsi tələb olunur.");
            }

            if (string.IsNullOrWhiteSpace(request.CardHolder))
            {
                throw new ValidationException("Kart sahibinin adı tələb olunur.");
            }
        }

        var payment = new Payment
        {
            Method = method,
            Amount = amount,
            Status = "pending",
            CardLast4 = digits.Length >= 4 ? digits[^4..] : string.Empty,
            CardHolder = needsCard ? request.CardHolder?.Trim() ?? string.Empty : string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        if (method == "hotel")
        {
            if (!request.DepositPercent.HasValue || !HotelDepositPercents.Contains(request.DepositPercent.Value))
            {
                throw new ValidationException("Oteldə ödəniş üçün 20%, 30% və ya 40% avans seçin və kartla ödəyin.");
            }

            ApplyDeposit(payment, amount, request.DepositPercent.Value);
        }
        else if (method == "card")
        {
            ApplyDeposit(payment, amount, 100);
        }

        return payment;
    }

    private static void ApplyDeposit(Payment payment, decimal amount, int percent)
    {
        payment.DepositPercent = percent;
        payment.DepositAmount = percent <= 0
            ? 0
            : Math.Round(amount * percent / 100m, 2, MidpointRounding.AwayFromZero);
        if (payment.DepositAmount > amount)
        {
            payment.DepositAmount = amount;
        }
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

    private async Task ApplyPricingAsync(Booking booking, decimal listTotal, BookingRequest request, bool applyAmount, CancellationToken cancellationToken)
    {
        var afterCombo = await ApplyComboPriceAsync(listTotal, request.BundleCombo, applyAmount, cancellationToken);
        var afterCoupon = afterCombo;
        var couponCode = string.Empty;
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var coupon = await _discounts.GetActiveByCodeAsync(request.CouponCode, cancellationToken)
                ?? throw new ValidationException("Kupon kodu keçərsizdir və ya müddəti bitib.");
            afterCoupon = ApplyPackage(afterCombo, coupon, applyAmount: true);
            couponCode = coupon.Code ?? string.Empty;
        }

        booking.TotalPrice = afterCoupon;
        booking.CouponCode = couponCode;
        booking.DiscountAmount = Math.Max(0, Math.Round(listTotal - afterCoupon, 2, MidpointRounding.AwayFromZero));
    }

    private async Task<decimal> ApplyComboPriceAsync(decimal total, string? combo, bool applyAmount, CancellationToken cancellationToken)
    {
        var package = await _discounts.GetActiveByComboAsync(combo, cancellationToken);
        return package is null ? total : ApplyPackage(total, package, applyAmount);
    }

    private static decimal ApplyPackage(decimal total, DiscountPackageResponse package, bool applyAmount)
    {
        if (package.Percent > 0)
        {
            return Math.Round(total * (1 - package.Percent / 100m), 2, MidpointRounding.AwayFromZero);
        }

        if (applyAmount && package.Amount > 0)
        {
            return Math.Max(0, total - package.Amount);
        }

        return total;
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

    private async Task<bool> TrySendConfirmationAsync(
        BookingResponse booking,
        CancellationToken cancellationToken,
        bool throwOnFailure = false)
    {
        if (!_email.DeliversToInbox || string.IsNullOrWhiteSpace(booking.GuestEmail))
        {
            if (throwOnFailure)
            {
                throw new ValidationException("Gmail SMTP şifrəsi yüklənməyib və ya qonaq email boşdur.");
            }

            return false;
        }

        try
        {
            var pdf = BookingReceiptPdf.Build(booking);
            await _email.SendAsync(
                booking.GuestEmail,
                $"AE Hotel rezervasiya təsdiqi {booking.Code}",
                BookingReceiptHtml.Build(booking),
                [
                    new EmailAttachment
                    {
                        FileName = $"rezervasiya-{booking.Code}.pdf",
                        ContentType = "application/pdf",
                        Content = pdf
                    }
                ],
                cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Booking confirmation email was not sent for {Code} to {Email}.", booking.Code, booking.GuestEmail);
            if (throwOnFailure)
            {
                throw new ValidationException(FriendlyMailError(exception, booking.GuestEmail));
            }

            return false;
        }
    }

    private static string FriendlyMailError(Exception exception, string email)
    {
        var text = $"{exception.Message} {exception.InnerException?.Message}";
        if (text.Contains("5.7.8", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("BadCredentials", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("not accepted", StringComparison.OrdinalIgnoreCase))
        {
            return "Gmail giriş qəbul olunmadı. App Password-u yenilə.";
        }

        return $"PDF {email} ünvanına göndərilmədi.";
    }

    private static decimal RemainingOf(Booking booking)
    {
        var deposit = booking.Payment?.DepositAmount ?? 0;
        var remaining = booking.TotalPrice - deposit;
        return remaining < 0 ? 0 : remaining;
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
            CouponCode = booking.CouponCode,
            DiscountAmount = booking.DiscountAmount,
            ItemName = booking.ItemName,
            Location = booking.Location,
            SpecialRequests = booking.SpecialRequests,
            PaymentMethod = booking.Payment?.Method ?? string.Empty,
            PaymentStatus = booking.Payment?.Status ?? string.Empty,
            DepositPercent = booking.Payment?.DepositPercent ?? 0,
            DepositAmount = booking.Payment?.DepositAmount ?? 0,
            RemainingAmount = RemainingOf(booking),
            PaymentId = booking.Payment?.Id,
            CreatedAt = booking.CreatedAt
        };
    }
}
