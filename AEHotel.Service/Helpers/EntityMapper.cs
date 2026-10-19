using AEHotel.Service.DTOs.Hotels;
using AEHotel.Service.DTOs.Rooms;
using AEHotel.Domain.Entities;

namespace AEHotel.Service.Helpers;

internal static class EntityMapper
{
    public static HotelResponse MapHotel(Hotel hotel, IReadOnlySet<int>? busyRoomIds = null, int? guests = null)
    {
        return new HotelResponse
        {
            Id = hotel.Id,
            Title = hotel.Title,
            City = hotel.City,
            Country = hotel.Country,
            Location = hotel.Location,
            Address = hotel.Address,
            Stars = hotel.Stars,
            Rating = hotel.Rating,
            ReviewCount = hotel.ReviewCount,
            Description = hotel.Description,
            PricePerNight = hotel.PricePerNight,
            HotelType = hotel.HotelType,
            CheckInTime = hotel.CheckInTime,
            CheckOutTime = hotel.CheckOutTime,
            ImageFile = hotel.ImageFile,
            Amenities = JsonLists.ReadStrings(hotel.AmenitiesJson),
            Rooms = hotel.Rooms.Select(room => MapRoom(room, IsRoomOpen(room, busyRoomIds, guests))).ToList()
        };
    }

    public static RoomResponse MapRoom(Room room, bool? isAvailable = null)
    {
        return new RoomResponse
        {
            Id = room.Id,
            HotelId = room.HotelId,
            HotelName = room.Hotel?.Title ?? string.Empty,
            Name = room.Name,
            Description = room.Description,
            Capacity = room.Capacity,
            BedType = room.BedType,
            Size = room.Size,
            PricePerNight = room.PricePerNight,
            Available = room.Available,
            IsAvailable = isAvailable ?? room.Available,
            ImageFile = room.ImageFile,
            Amenities = JsonLists.ReadStrings(room.AmenitiesJson)
        };
    }

    internal static bool IsRoomOpen(Room room, IReadOnlySet<int>? busyRoomIds, int? guests)
    {
        if (!room.Available)
        {
            return false;
        }

        if (guests.HasValue && guests.Value > room.Capacity)
        {
            return false;
        }

        return busyRoomIds == null || !busyRoomIds.Contains(room.Id);
    }
}
