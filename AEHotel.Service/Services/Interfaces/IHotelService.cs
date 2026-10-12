using AEHotel.Service.DTOs.Hotels;

namespace AEHotel.Service.Services.Interfaces;

public interface IHotelService
{
    Task<IReadOnlyList<HotelResponse>> GetAllAsync(DateOnly? checkIn = null, DateOnly? checkOut = null, int? guests = null, CancellationToken cancellationToken = default);

    Task<HotelResponse> GetByIdAsync(int id, DateOnly? checkIn = null, DateOnly? checkOut = null, int? guests = null, CancellationToken cancellationToken = default);

    Task<HotelResponse> CreateAsync(HotelRequest request, CancellationToken cancellationToken = default);

    Task<HotelResponse> UpdateAsync(int id, HotelRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
