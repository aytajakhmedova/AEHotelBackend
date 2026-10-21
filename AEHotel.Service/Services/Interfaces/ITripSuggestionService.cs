using AEHotel.Service.DTOs.Bookings;

namespace AEHotel.Service.Services.Interfaces;

public interface ITripSuggestionService
{
    Task<TripSuggestionResponse> GetAsync(
        string? city,
        DateOnly? checkIn,
        DateOnly? checkOut,
        int guests = 1,
        CancellationToken cancellationToken = default);
}
