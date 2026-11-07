using AEHotel.Service.DTOs.SeasonalPrices;

namespace AEHotel.Service.Services.Interfaces;

public interface ISeasonalPriceService
{
    Task<IReadOnlyList<SeasonalPriceResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SeasonalPriceResponse> CreateAsync(SeasonalPriceRequest request, CancellationToken cancellationToken = default);

    Task<SeasonalPriceResponse> UpdateAsync(int id, SeasonalPriceRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
