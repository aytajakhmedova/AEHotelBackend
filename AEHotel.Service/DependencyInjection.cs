using AEHotel.Service.Hotels;
using AEHotel.Service.Reservations;
using AEHotel.Service.Rooms;
using Microsoft.Extensions.DependencyInjection;

namespace AEHotel.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IHotelService, HotelService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IReservationService, ReservationService>();

        return services;
    }
}
