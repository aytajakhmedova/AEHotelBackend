using AEHotel.Service.DTOs.Bookings;

namespace AEHotel.Service.Services.Interfaces;

public interface ITripSuggestionService
{
    Task<TripSuggestionResponse> GetAsync(
        string? city,
        string? country,
        DateOnly? checkIn,
        DateOnly? checkOut,
        int guests = 1,
        CancellationToken cancellationToken = default);

    Task<TripSuggestionResponse> GetByHotelAsync(
        int hotelId,
        DateOnly? checkIn,
        DateOnly? checkOut,
        int guests = 1,
        CancellationToken cancellationToken = default);

    Task<TripSuggestionResponse> GetByCountryAsync(
        string country,
        int guests = 1,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TripCountryResponse>> GetCountriesAsync(CancellationToken cancellationToken = default);
}
