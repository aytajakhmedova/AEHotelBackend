using AEHotel.Service.DTOs.Discounts;

namespace AEHotel.Service.Services.Interfaces;

public interface IDiscountService
{
    Task<IReadOnlyList<DiscountPackageResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscountPackageResponse>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<decimal> GetActivePercentAsync(string? combo, CancellationToken cancellationToken = default);

    Task<DiscountPackageResponse?> GetActiveByComboAsync(string? combo, CancellationToken cancellationToken = default);

    /// <summary>The valid coupon for the code. A personal coupon is only valid for its owner (userId) and only until it has been used.</summary>
    Task<DiscountPackageResponse?> GetActiveByCodeAsync(string? code, CancellationToken cancellationToken = default, string? userId = null);

    /// <summary>Creates a personal coupon for a newly registered user.</summary>
    Task<DiscountPackageResponse> CreateWelcomeCouponAsync(string userId, decimal percent, int validMinutes, CancellationToken cancellationToken = default);

    /// <summary>Marks a personal coupon as used (the first time only). Call it when a booking takes the discount.</summary>
    Task ConsumeAsync(string? code, CancellationToken cancellationToken = default);

    /// <summary>The user's own coupons, used ones included so they can be shown as used.</summary>
    Task<IReadOnlyList<DiscountPackageResponse>> GetMineAsync(string userId, CancellationToken cancellationToken = default);

    Task<DiscountPackageResponse> CreateAsync(DiscountPackageRequest request, CancellationToken cancellationToken = default);

    Task<DiscountPackageResponse> UpdateAsync(int id, DiscountPackageRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
