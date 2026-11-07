using AEHotel.Service.Services.Interfaces;
using AEHotel.Service.DTOs.Bookings;
using AEHotel.Service.DTOs.Discounts;
using AEHotel.Service.DTOs.Email;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

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

        // Use serializable transaction for booking creation to prevent race conditions
        var strategy = _bookings.GetExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _bookings.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            try
            {
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
                await transaction.CommitAsync(cancellationToken);

                var saved = await _bookings.GetByIdAsync(booking.Id, tracking: false, cancellationToken);
                var response = await MapWithSuggestionsAsync(saved!, cancellationToken);
                response.EmailSent = await TrySendConfirmationAsync(response, cancellationToken);
                return response;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning(ex, "Concurrency conflict during booking creation for user {UserId}", userId);
                throw new ValidationException("Bu rezervasiya artıq başqa istifadəçi tərəfindən edilib. Zəhmət olmasa yenidən cəhd edin.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
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
            RoomId = booking.Rooms.Count == 0 ? booking.RoomId : null,
            RoomIds = booking.Rooms.Count == 0 ? null : booking.Rooms.Select(r => r.RoomId).ToList(),
            Children = Math.Min(booking.Rooms.Sum(r => r.Children), Math.Max(0, request.Guests - 1)),
            GuestName = booking.GuestName,
            GuestEmail = booking.GuestEmail,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            Guests = request.Guests,
            SpecialRequests = request.SpecialRequests
        };

        // Use serializable transaction for update to prevent race conditions
        var strategy = _bookings.GetExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _bookings.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            try
            {
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
                await transaction.CommitAsync(cancellationToken);

                var saved = await _bookings.GetByIdAsync(id, tracking: false, cancellationToken);
                return Map(saved!);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning(ex, "Concurrency conflict during booking update for booking {BookingId}", id);
                throw new ValidationException("Bu rezervasiya artıq dəyişdirilib. Zəhmət olmasa səhifəni yeniləyib yenidən cəhd edin.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
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

            // Use serializable transaction for cancellation to prevent race conditions
            var strategy = _bookings.GetExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _bookings.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
                try
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

                    booking.Status = next;
                    await _bookings.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    var saved = await _bookings.GetByIdAsync(id, tracking: false, cancellationToken);
                    return Map(saved!);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogWarning(ex, "Concurrency conflict during booking cancellation for booking {BookingId}", id);
                    throw new ValidationException("Bu rezervasiya artıq dəyişdirilib. Zəhmət olmasa səhifəni yeniləyib yenidən cəhd edin.");
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
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

    public Task<HotelStayQuote> QuoteHotelAsync(int hotelId, HotelStayRequest request, CancellationToken cancellationToken = default)
        => QuoteHotelAsync(hotelId, request, null, false, cancellationToken);

    private async Task<HotelStayQuote> QuoteHotelAsync(int hotelId, HotelStayRequest request, int? excludeBookingId, bool forBooking, CancellationToken cancellationToken)
    {
        if (request.CheckIn is not DateOnly arrival || request.CheckOut is not DateOnly departure ||
            arrival < DateOnly.FromDateTime(DateTime.UtcNow) || departure <= arrival)
            throw new ValidationException("Valid arrival and departure dates are required.");

        var ids = request.RoomIds ?? (request.RoomId.HasValue ? [request.RoomId.Value] : []);
        if (ids.Count == 0 || ids.Any(id => id < 1) || ids.Distinct().Count() != ids.Count ||
            (request.RoomId.HasValue && (ids.Count != 1 || request.RoomId != ids[0])))
            throw new ValidationException("Select distinct room IDs from one hotel.");
        var party = StayParty.From(request.Adults, request.Children, request.Rooms ?? ids.Count, request.Guests);
        if (party.Rooms != ids.Count)
            throw new ValidationException("The number of selected rooms must match the requested count.");

        if (!await _hotels.ExistsAsync(hotelId, cancellationToken))
            throw new NotFoundException($"Hotel with id {hotelId} was not found.");

        var selected = new List<Room>(ids.Count);
        // Consistent lock order for overlapping selections in the create/update transaction.
        var sortedIds = ids.Order().ToArray();
        if (forBooking)
        {
            foreach (int id in sortedIds)
                await _bookings.LockRoomForBookingAsync(id, cancellationToken);
        }
        foreach (int id in sortedIds)
        {
            var room = await _rooms.GetByIdAsync(id, false, cancellationToken)
                ?? throw new NotFoundException($"Room {id} was not found.");
            if (room.HotelId != hotelId || room.IsDeleted || room.Hotel?.IsDeleted != false || !room.Available)
                throw new ValidationException($"Room {id} is not available at this hotel.");
            if (await _bookings.HasHotelOverlapAsync(id, arrival, departure, excludeBookingId, cancellationToken) ||
                await _reservations.HasOverlapAsync(id, arrival, departure, null, cancellationToken))
                throw new ValidationException($"Room {id} is already reserved for these dates.");
            selected.Add(room);
        }
        var allocation = party.Allocate(selected.Select(room => room.Capacity).ToList());
        var lines = selected.Select((room, index) =>
        {
            var (adults, children) = allocation[index];
            var price = StayPriceCalculator.Calculate(room.PricePerNight, arrival, departure, adults + children);
            return new StayRoom(room.Id, room.Name, adults, children, room.PricePerNight, price.Total);
        }).ToList();
        return new HotelStayQuote(departure.DayNumber - arrival.DayNumber, lines.Sum(line => line.Total), lines);
    }

    private async Task ApplyHotelAsync(Booking booking, BookingRequest request, int? excludeBookingId, CancellationToken cancellationToken)
    {
        int hotelId = request.HotelId ?? throw new ValidationException("Hotel is required.");
        var quote = await QuoteHotelAsync(hotelId, request, excludeBookingId, true, cancellationToken);
        if (request.ExpectedStayTotal is decimal expected && expected != quote.Total)
            throw new ValidationException("Qiymət dəyişib. Otaq seçiminə qayıdaraq yeni qiyməti təsdiqləyin.");

        foreach (var line in quote.Rooms)
        {
            var stay = booking.Rooms.SingleOrDefault(r => r.RoomId == line.RoomId);
            if (stay is null)
            {
                stay = new BookingRoom { RoomId = line.RoomId };
                booking.Rooms.Add(stay);
            }
            stay.RoomName = line.RoomName;
            stay.Adults = line.Adults;
            stay.Children = line.Children;
            stay.PricePerNight = line.PricePerNight;
        }
        booking.HotelId = hotelId;
        booking.RoomId = quote.Rooms[0].RoomId;
        booking.CheckIn = request.CheckIn;
        booking.CheckOut = request.CheckOut;
        booking.Guests = quote.Rooms.Sum(line => line.Adults + line.Children);
        booking.Nights = quote.Nights;
        booking.PricePerNight = quote.Rooms.Sum(line => line.PricePerNight);
        await ApplyPricingAsync(booking, quote.Total, request, applyAmount: true, cancellationToken);
        booking.ItemName = quote.Rooms[0].RoomName;
        booking.Location = await _hotels.GetTitleAsync(hotelId, cancellationToken) ?? string.Empty;
    }

    private async Task ApplyFlightAsync(Booking booking, BookingRequest request, CancellationToken cancellationToken)
    {
        if (!request.FlightId.HasValue)
        {
            throw new ValidationException("Flight is required.");
        }

        var flight = await _flights.GetByIdAsync(request.FlightId.Value, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Flight with id {request.FlightId} was not found.");

        if (flight.DepartureAt <= DateTime.UtcNow)
        {
            throw new ValidationException("Keçmiş tarixli uçuş rezervasiya edilə bilməz.");
        }

        if (!string.IsNullOrWhiteSpace(request.TravelCountry) &&
            !TravelCountry.Same(TravelCountry.OfFlight(flight), request.TravelCountry))
        {
            throw new ValidationException("Seçilmiş uçuş bu ölkəyə aid deyil.");
        }

        // The guest chooses the class; a flight that sells a single class needs no choice.
        var wanted = FlightCabins.Normalize(request.CabinClass);
        if (wanted == null && !string.IsNullOrWhiteSpace(request.CabinClass))
        {
            throw new ValidationException("Səyahət sinfi economy, business və ya first olmalıdır.");
        }

        var cabin = wanted != null
            ? flight.Cabins.FirstOrDefault(item => item.CabinClass == wanted)
            : flight.Cabins.Count == 1 ? flight.Cabins[0] : null;
        if (cabin == null)
        {
            throw new ValidationException(wanted == null
                ? "Zəhmət olmasa səyahət sinfini seçin."
                : "Bu uçuşda seçilən sinif satılmır.");
        }

        if (cabin.SeatsAvailable < request.Guests)
        {
            throw new ValidationException("Seçilən sinifdə kifayət qədər boş yer yoxdur.");
        }

        cabin.SeatsAvailable -= request.Guests;
        flight.SeatsAvailable = flight.Cabins.Sum(item => item.SeatsAvailable);
        booking.CabinClass = cabin.CabinClass;
        booking.FlightId = flight.Id;
        booking.CheckIn = DateOnly.FromDateTime(flight.DepartureAt);
        booking.Guests = request.Guests;
        booking.Nights = 0;
        booking.PricePerNight = cabin.Price;
        await ApplyPricingAsync(booking, cabin.Price * request.Guests, request, applyAmount: true, cancellationToken);
        booking.ItemName = $"{flight.Airline} {flight.FlightNumber}";
        booking.Location = $"{flight.OriginCity} ({flight.OriginCode}) - {flight.DestinationCity} ({flight.DestinationCode})";
    }

    private async Task ApplyTourAsync(Booking booking, BookingRequest request, int? excludeBookingId, CancellationToken cancellationToken)
    {
        if (!request.TourId.HasValue || !request.TravelDate.HasValue)
        {
            throw new ValidationException("Tour and travel date are required.");
        }

        if (request.TravelDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ValidationException("Keçmiş tarixli tur rezervasiya edilə bilməz.");
        }

        var tour = await _tours.GetByIdAsync(request.TourId.Value, tracking: false, cancellationToken)
            ?? throw new NotFoundException($"Tour with id {request.TourId} was not found.");

        if (!string.IsNullOrWhiteSpace(request.TravelCountry) &&
            !TravelCountry.Same(TravelCountry.OfTour(tour), request.TravelCountry))
        {
            throw new ValidationException("Seçilmiş tur bu ölkəyə aid deyil.");
        }

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
            var cabin = flight?.Cabins.FirstOrDefault(item => item.CabinClass == booking.CabinClass)
                ?? flight?.Cabins.FirstOrDefault();
            if (flight != null && cabin != null)
            {
                cabin.SeatsAvailable += booking.Guests;
                flight.SeatsAvailable = flight.Cabins.Sum(item => item.SeatsAvailable);
            }
        }
    }

    private static readonly int[] HotelDepositPercents = [20, 30, 40];

    private static Payment CreatePayment(BookingRequest request, decimal amount)
    {
        var method = NormalizePaymentMethod(request.PaymentMethod);
        
        // Demo payment system - we don't store real card details
        // Only validate that card info is provided for card/hotel payments
        if (method is "card" or "hotel")
        {
            var digits = new string((request.CardNumber ?? string.Empty).Where(char.IsDigit).ToArray());
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
            CardLast4 = string.Empty, // Demo system - don't store card details
            CardHolder = string.Empty, // Demo system - don't store cardholder name
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
        // BundleCombo discount cannot be securely verified in backend because each booking is created separately
        // Hotel bookings don't include FlightId/TourId, and vice versa
        // Only coupon codes are verified and applied
        var afterCombo = listTotal; // Ignore BundleCombo for security - frontend handles package pricing separately
        var afterCoupon = afterCombo;
        var couponCode = string.Empty;
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var coupon = await _discounts.GetActiveByCodeAsync(request.CouponCode, cancellationToken, booking.UserId)
                ?? throw new ValidationException("Kupon kodu keçərsizdir və ya müddəti bitib.");
            // A personal coupon is used up by its first booking (stored in the database).
            await _discounts.ConsumeAsync(request.CouponCode, cancellationToken);
            afterCoupon = ApplyPackage(afterCombo, coupon, applyAmount: true);
            couponCode = coupon.Code ?? string.Empty;
        }

        booking.TotalPrice = afterCoupon;
        booking.CouponCode = couponCode;
        booking.DiscountAmount = Math.Max(0, Math.Round(listTotal - afterCoupon, 2, MidpointRounding.AwayFromZero));
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
            booking.Hotel?.Country,
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
        var rooms = booking.Rooms.OrderBy(r => r.RoomId).Select(r => new StayRoom(r.RoomId, r.RoomName,
            r.Adults, r.Children, r.PricePerNight, StayPriceCalculator.Calculate(r.PricePerNight,
                booking.CheckIn!.Value, booking.CheckOut!.Value, r.Adults + r.Children).Total)).ToList();
        if (rooms.Count == 0 && booking.RoomId is int legacyId && booking.CheckIn is DateOnly arrival && booking.CheckOut is DateOnly departure)
            rooms.Add(new StayRoom(legacyId, booking.Room?.Name ?? booking.ItemName, booking.Guests, 0,
                booking.PricePerNight, StayPriceCalculator.Calculate(booking.PricePerNight, arrival, departure, booking.Guests).Total));
        var roomNames = rooms.Count > 0 ? string.Join(", ", rooms.Select(r => r.RoomName)) : booking.ItemName;
        return new BookingResponse
        {
            Id = booking.Id,
            Code = booking.Code,
            BookingType = booking.BookingType,
            Status = booking.Status,
            HotelId = booking.HotelId,
            HotelName = booking.Hotel?.Title ?? (booking.BookingType == "hotel" ? booking.Location : string.Empty),
            RoomId = booking.RoomId,
            Rooms = rooms,
            RoomName = roomNames,
            FlightId = booking.FlightId,
            FlightNumber = booking.Flight?.FlightNumber ?? string.Empty,
            CabinClass = booking.CabinClass,
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
            ItemName = rooms.Count > 0 ? roomNames : booking.ItemName,
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
