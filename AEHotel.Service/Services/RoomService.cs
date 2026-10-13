using AEHotel.Service.Services.Interfaces;
using AEHotel.Service.DTOs.Rooms;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Helpers;

namespace AEHotel.Service.Services;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _rooms;
    private readonly IHotelRepository _hotels;
    private readonly IBookingRepository _bookings;
    private readonly IReservationRepository _reservations;

    public RoomService(IRoomRepository rooms, IHotelRepository hotels, IBookingRepository bookings, IReservationRepository reservations)
    {
        _rooms = rooms;
        _hotels = hotels;
        _bookings = bookings;
        _reservations = reservations;
    }

    public async Task<IReadOnlyList<RoomResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rooms = await _rooms.GetAllAsync(cancellationToken);
        return rooms.Select(room => EntityMapper.MapRoom(room)).ToList();
    }

    public async Task<IReadOnlyList<RoomResponse>> GetByHotelIdAsync(int hotelId, CancellationToken cancellationToken = default)
    {
        if (!await _hotels.ExistsAsync(hotelId, cancellationToken))
        {
            throw new NotFoundException($"Hotel with id {hotelId} was not found.");
        }

        var rooms = await _rooms.GetByHotelIdAsync(hotelId, cancellationToken);
        return rooms.Select(room => EntityMapper.MapRoom(room)).ToList();
    }

    public async Task<RoomResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetByIdAsync(id, tracking: false, cancellationToken)
            ?? throw new NotFoundException($"Room with id {id} was not found.");

        return EntityMapper.MapRoom(room);
    }

    public async Task<RoomResponse> CreateAsync(RoomRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var room = new Room();
        Apply(room, request);
        await _rooms.AddAsync(room, cancellationToken);
        await _rooms.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(room.Id, cancellationToken);
    }

    public async Task<RoomResponse> UpdateAsync(int id, RoomRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var room = await _rooms.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Room with id {id} was not found.");

        Apply(room, request);
        await _rooms.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Room with id {id} was not found.");

        if (await _rooms.HasReservationsAsync(id, cancellationToken) ||
            await _bookings.ExistsForRoomAsync(id, cancellationToken))
        {
            throw new ValidationException("This room cannot be deleted because it has reservations.");
        }

        _rooms.Remove(room);
        await _rooms.SaveChangesAsync(cancellationToken);
    }

    public async Task<RoomQuoteResponse> QuoteAsync(int id, DateOnly checkIn, DateOnly checkOut, int guests, int? excludeBookingId = null, CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetByIdAsync(id, tracking: false, cancellationToken)
            ?? throw new NotFoundException($"Room with id {id} was not found.");

        if (checkOut <= checkIn)
        {
            return Unavailable("Check-out date must be after the check-in date.");
        }

        if (guests < 1)
        {
            return Unavailable("At least one guest is required.");
        }

        if (guests > room.Capacity)
        {
            return Unavailable($"This room allows at most {room.Capacity} guests.");
        }

        if (!room.Available)
        {
            return Unavailable("This room is not available for booking.");
        }

        if (await _bookings.HasHotelOverlapAsync(room.Id, checkIn, checkOut, excludeBookingId, cancellationToken) ||
            await _reservations.HasOverlapAsync(room.Id, checkIn, checkOut, excludeReservationId: null, cancellationToken))
        {
            return Unavailable("The room is already reserved for the selected dates.");
        }

        var price = StayPriceCalculator.Calculate(room.PricePerNight, checkIn, checkOut, guests);
        return new RoomQuoteResponse
        {
            Available = true,
            Nights = price.Nights,
            BasePrice = price.BasePrice,
            ExtraGuestFee = price.ExtraGuestFee,
            Subtotal = price.Subtotal,
            TaxRate = StayPriceCalculator.TaxRate,
            Tax = price.Tax,
            Total = price.Total
        };
    }

    private static RoomQuoteResponse Unavailable(string message)
    {
        return new RoomQuoteResponse
        {
            Available = false,
            Message = message
        };
    }

    private async Task ValidateAsync(RoomRequest request, CancellationToken cancellationToken)
    {
        if (request.HotelId < 1)
        {
            throw new ValidationException("Hotel is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Room name is required.");
        }

        if (request.Capacity < 1)
        {
            throw new ValidationException("Room capacity must be at least 1.");
        }

        if (request.PricePerNight < 0)
        {
            throw new ValidationException("Price per night cannot be negative.");
        }

        if (!await _hotels.ExistsAsync(request.HotelId, cancellationToken))
        {
            throw new NotFoundException($"Hotel with id {request.HotelId} was not found.");
        }
    }

    private static void Apply(Room room, RoomRequest request)
    {
        room.HotelId = request.HotelId;
        room.Name = request.Name.Trim();
        room.Description = request.Description.Trim();
        room.Capacity = request.Capacity;
        room.BedType = request.BedType.Trim();
        room.Size = request.Size.Trim();
        room.PricePerNight = request.PricePerNight;
        room.Available = request.Available;
        room.ImageFile = request.ImageFile.Trim();
        room.AmenitiesJson = JsonLists.WriteStrings(request.Amenities);
    }
}
