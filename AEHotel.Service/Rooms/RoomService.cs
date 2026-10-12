using AEHotel.App.Rooms;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Hotels;
using AEHotel.Repository.Rooms;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Mapping;

namespace AEHotel.Service.Rooms;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _rooms;
    private readonly IHotelRepository _hotels;

    public RoomService(IRoomRepository rooms, IHotelRepository hotels)
    {
        _rooms = rooms;
        _hotels = hotels;
    }

    public async Task<IReadOnlyList<RoomResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rooms = await _rooms.GetAllAsync(cancellationToken);
        return rooms.Select(EntityMapper.MapRoom).ToList();
    }

    public async Task<IReadOnlyList<RoomResponse>> GetByHotelIdAsync(int hotelId, CancellationToken cancellationToken = default)
    {
        if (!await _hotels.ExistsAsync(hotelId, cancellationToken))
        {
            throw new NotFoundException($"Hotel with id {hotelId} was not found.");
        }

        var rooms = await _rooms.GetByHotelIdAsync(hotelId, cancellationToken);
        return rooms.Select(EntityMapper.MapRoom).ToList();
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

        return EntityMapper.MapRoom(room);
    }

    public async Task<RoomResponse> UpdateAsync(int id, RoomRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var room = await _rooms.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Room with id {id} was not found.");

        Apply(room, request);
        await _rooms.SaveChangesAsync(cancellationToken);

        return EntityMapper.MapRoom(room);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Room with id {id} was not found.");

        if (await _rooms.HasReservationsAsync(id, cancellationToken))
        {
            throw new ValidationException("This room cannot be deleted because it has reservations.");
        }

        _rooms.Remove(room);
        await _rooms.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateAsync(RoomRequest request, CancellationToken cancellationToken)
    {
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
    }
}
