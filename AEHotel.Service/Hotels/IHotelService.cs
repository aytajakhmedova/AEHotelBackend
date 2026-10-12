using AEHotel.App.Hotels;

namespace AEHotel.Service.Hotels;

public interface IHotelService
{
    Task<IReadOnlyList<HotelResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<HotelResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<HotelResponse> CreateAsync(HotelRequest request, CancellationToken cancellationToken = default);

    Task<HotelResponse> UpdateAsync(int id, HotelRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
