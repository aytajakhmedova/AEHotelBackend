using AEHotel.Service.DTOs.Hotels;

namespace AEHotel.Service.Services.Interfaces;

public interface IHotelService
{
    /// <summary>The hotel types and amenities the hotels really have (for the filter lists).</summary>
    Task<HotelFilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HotelResponse>> GetAllAsync(string? city = null, DateOnly? checkIn = null, DateOnly? checkOut = null, int? guests = null, CancellationToken cancellationToken = default, int? adults = null, int? children = null, int? rooms = null, string? country = null, decimal? minPrice = null, decimal? maxPrice = null, string? currency = null, string? stars = null, string? hotelTypes = null, string? amenities = null);

    Task<HotelResponse> GetByIdAsync(int id, DateOnly? checkIn = null, DateOnly? checkOut = null, int? guests = null, CancellationToken cancellationToken = default, int? adults = null, int? children = null, int? rooms = null);

    Task<HotelResponse> CreateAsync(HotelRequest request, CancellationToken cancellationToken = default);

    Task<HotelResponse> UpdateAsync(int id, HotelRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
