using AEHotel.Service.Services;
using AEHotel.Service.Services.Interfaces;
using AEHotel.Service.Security;
using Microsoft.Extensions.DependencyInjection;

namespace AEHotel.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IHotelService, HotelService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IFlightService, FlightService>();
        services.AddScoped<IAirlineService, AirlineService>();
        services.AddScoped<ITourCategoryService, TourCategoryService>();
        services.AddScoped<ITourService, TourService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<ITripSuggestionService, TripSuggestionService>();
        services.AddScoped<ISiteContentService, SiteContentService>();
        services.AddScoped<ISeasonalPriceService, SeasonalPriceService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<IStayReviewService, StayReviewService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<UserManagementPolicy>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
