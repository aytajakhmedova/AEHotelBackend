using AEHotel.Service.Services;
using AEHotel.Service.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AEHotel.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IHotelService, HotelService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IFlightService, FlightService>();
        services.AddScoped<ITourService, TourService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<ITripSuggestionService, TripSuggestionService>();
        services.AddScoped<ISiteContentService, SiteContentService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<IStayReviewService, StayReviewService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
