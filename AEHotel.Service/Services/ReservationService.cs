using AEHotel.Service.Services.Interfaces;
using AEHotel.Service.DTOs.Reservations;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Helpers;

namespace AEHotel.Service.Services;

public class ReservationService : IReservationService
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "confirmed",
        "upcoming",
        "checked-in",
        "completed",
        "cancelled"
    };

    private readonly IReservationRepository _reservations;
    private readonly IHotelRepository _hotels;
    private readonly IRoomRepository _rooms;

    public ReservationService(
        IReservationRepository reservations,
        IHotelRepository hotels,
        IRoomRepository rooms)
    {
        _reservations = reservations;
        _hotels = hotels;
        _rooms = rooms;
    }

    public async Task<IReadOnlyList<ReservationResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var reservations = await _reservations.GetAllAsync(cancellationToken);
        return reservations.Select(reservation => EntityMapper.MapReservation(reservation)).ToList();
    }

    public async Task<ReservationResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservations.GetByIdAsync(id, tracking: false, includeDetails: true, cancellationToken)
            ?? throw new NotFoundException($"Reservation with id {id} was not found.");

        return EntityMapper.MapReservation(reservation);
    }

    public async Task<ReservationResponse> CreateAsync(ReservationRequest request, CancellationToken cancellationToken = default)
    {
        var room = await ValidateAndGetRoomAsync(request, excludeReservationId: null, cancellationToken);
        var reservation = BuildReservation(request, room);

        await _reservations.AddAsync(reservation, cancellationToken);
        await _reservations.SaveChangesAsync(cancellationToken);

        var hotelName = await _hotels.GetTitleAsync(reservation.HotelId, cancellationToken) ?? string.Empty;
        return EntityMapper.MapReservation(reservation, hotelName, room.Name);
    }

    public async Task<ReservationResponse> UpdateAsync(int id, ReservationRequest request, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservations.GetByIdAsync(id, tracking: true, includeDetails: false, cancellationToken)
            ?? throw new NotFoundException($"Reservation with id {id} was not found.");

        var room = await ValidateAndGetRoomAsync(request, excludeReservationId: id, cancellationToken);
        Apply(reservation, request, room);
        await _reservations.SaveChangesAsync(cancellationToken);

        var hotelName = await _hotels.GetTitleAsync(reservation.HotelId, cancellationToken) ?? string.Empty;
        return EntityMapper.MapReservation(reservation, hotelName, room.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservations.GetByIdAsync(id, tracking: true, includeDetails: false, cancellationToken)
            ?? throw new NotFoundException($"Reservation with id {id} was not found.");

        _reservations.Remove(reservation);
        await _reservations.SaveChangesAsync(cancellationToken);
    }

    private async Task<Room> ValidateAndGetRoomAsync(
        ReservationRequest request,
        int? excludeReservationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.GuestName))
        {
            throw new ValidationException("Guest name is required.");
        }

        if (!IsValidEmail(request.GuestEmail))
        {
            throw new ValidationException("A valid guest email is required.");
        }

        if (request.Guests < 1)
        {
            throw new ValidationException("At least one guest is required.");
        }

        if (request.CheckOut <= request.CheckIn)
        {
            throw new ValidationException("Check-out date must be after the check-in date.");
        }

        var status = NormalizeStatus(request.Status);
        if (!AllowedStatuses.Contains(status))
        {
            throw new ValidationException("Status must be confirmed, upcoming, checked-in, completed, or cancelled.");
        }

        if (!await _hotels.ExistsAsync(request.HotelId, cancellationToken))
        {
            throw new NotFoundException($"Hotel with id {request.HotelId} was not found.");
        }

        var room = await _rooms.GetByIdAsync(request.RoomId, tracking: false, cancellationToken)
            ?? throw new NotFoundException($"Room with id {request.RoomId} was not found.");

        if (room.HotelId != request.HotelId)
        {
            throw new ValidationException("The selected room does not belong to the selected hotel.");
        }

        if (request.Guests > room.Capacity)
        {
            throw new ValidationException($"This room allows at most {room.Capacity} guests.");
        }

        if (!room.Available && !string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("This room is not available for booking.");
        }

        if (!string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase) &&
            await _reservations.HasOverlapAsync(room.Id, request.CheckIn, request.CheckOut, excludeReservationId, cancellationToken))
        {
            throw new ValidationException("The room is already reserved for the selected dates.");
        }

        return room;
    }

    private static Reservation BuildReservation(ReservationRequest request, Room room)
    {
        var reservation = new Reservation
        {
            Code = CreateCode(),
            CreatedAt = DateTime.UtcNow
        };

        Apply(reservation, request, room);
        return reservation;
    }

    private static void Apply(Reservation reservation, ReservationRequest request, Room room)
    {
        var nights = request.CheckOut.DayNumber - request.CheckIn.DayNumber;

        reservation.Status = NormalizeStatus(request.Status);
        reservation.HotelId = request.HotelId;
        reservation.RoomId = request.RoomId;
        reservation.GuestName = request.GuestName.Trim();
        reservation.GuestEmail = request.GuestEmail.Trim();
        reservation.CheckIn = request.CheckIn;
        reservation.CheckOut = request.CheckOut;
        reservation.Guests = request.Guests;
        reservation.PricePerNight = room.PricePerNight;
        reservation.TotalPrice = room.PricePerNight * nights;
    }

    private static string NormalizeStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? "confirmed"
            : status.Trim().ToLowerInvariant();
    }

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var value = email.Trim();
        var at = value.IndexOf('@');
        var dot = value.LastIndexOf('.');
        return at > 0 && dot > at + 1 && dot < value.Length - 1 && !value.Contains(' ');
    }

    private static string CreateCode()
    {
        return $"RES-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }
}
