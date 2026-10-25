using AEHotel.Service.DTOs.Discounts;

namespace AEHotel.Service.Services.Interfaces;

public interface IDiscountService
{
    Task<IReadOnlyList<DiscountPackageResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscountPackageResponse>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<decimal> GetActivePercentAsync(string? combo, CancellationToken cancellationToken = default);

    Task<DiscountPackageResponse?> GetActiveByComboAsync(string? combo, CancellationToken cancellationToken = default);

    Task<DiscountPackageResponse> CreateAsync(DiscountPackageRequest request, CancellationToken cancellationToken = default);

    Task<DiscountPackageResponse> UpdateAsync(int id, DiscountPackageRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
