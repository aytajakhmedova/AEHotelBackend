using AEHotel.Domain.Entities;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.DTOs.Discounts;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Services.Interfaces;

namespace AEHotel.Service.Services;

public class DiscountService : IDiscountService
{
    public static readonly string[] Combos = ["hotel_flight", "hotel_tour", "hotel_flight_tour"];

    private readonly IDiscountPackageRepository _packages;

    public DiscountService(IDiscountPackageRepository packages)
    {
        _packages = packages;
    }

    public async Task<IReadOnlyList<DiscountPackageResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _packages.GetAllAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<DiscountPackageResponse>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var items = await _packages.GetAllAsync(cancellationToken);
        return items
            .Where(item => item.IsActive)
            .Where(item => !item.StartsAt.HasValue || item.StartsAt <= now)
            .Where(item => !item.EndsAt.HasValue || item.EndsAt >= now)
            .Select(Map)
            .ToList();
    }

    public async Task<decimal> GetActivePercentAsync(string? combo, CancellationToken cancellationToken = default)
    {
        var key = NormalizeCombo(combo);
        if (key is null)
        {
            return 0;
        }

        var package = await GetActiveByComboAsync(key, cancellationToken);
        return package?.Percent ?? 0;
    }

    public async Task<DiscountPackageResponse?> GetActiveByComboAsync(string? combo, CancellationToken cancellationToken = default)
    {
        var key = NormalizeCombo(combo);
        if (key is null)
        {
            return null;
        }

        var package = await _packages.GetActiveByComboAsync(key, DateTime.UtcNow, cancellationToken);
        return package is null ? null : Map(package);
    }

    public async Task<DiscountPackageResponse> CreateAsync(DiscountPackageRequest request, CancellationToken cancellationToken = default)
    {
        var package = new DiscountPackage();
        Apply(package, request);
        await _packages.AddAsync(package, cancellationToken);
        await _packages.SaveChangesAsync(cancellationToken);
        return Map(package);
    }

    public async Task<DiscountPackageResponse> UpdateAsync(int id, DiscountPackageRequest request, CancellationToken cancellationToken = default)
    {
        var package = await _packages.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Discount package with id {id} was not found.");
        Apply(package, request);
        await _packages.SaveChangesAsync(cancellationToken);
        return Map(package);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var package = await _packages.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Discount package with id {id} was not found.");
        _packages.Remove(package);
        await _packages.SaveChangesAsync(cancellationToken);
    }

    public static string? NormalizeCombo(string? combo)
    {
        var value = (combo ?? string.Empty).Trim().ToLowerInvariant();
        return Combos.Contains(value) ? value : null;
    }

    private static void Apply(DiscountPackage package, DiscountPackageRequest request)
    {
        var combo = NormalizeCombo(request.Combo)
            ?? throw new ValidationException("Combo must be hotel_flight, hotel_tour, or hotel_flight_tour.");
        if (request.Percent < 0 || request.Percent > 100)
        {
            throw new ValidationException("Percent must be between 0 and 100.");
        }

        if (request.Amount < 0)
        {
            throw new ValidationException("Amount cannot be negative.");
        }

        package.Name = string.IsNullOrWhiteSpace(request.Name) ? combo : request.Name.Trim();
        package.Combo = combo;
        package.Percent = request.Percent;
        package.Amount = request.Amount;
        package.StartsAt = request.StartsAt;
        package.EndsAt = request.EndsAt;
        package.IsActive = request.IsActive;
    }

    private static DiscountPackageResponse Map(DiscountPackage package)
    {
        return new DiscountPackageResponse
        {
            Id = package.Id,
            Name = package.Name,
            Combo = package.Combo,
            Percent = package.Percent,
            Amount = package.Amount,
            StartsAt = package.StartsAt,
            EndsAt = package.EndsAt,
            IsActive = package.IsActive
        };
    }
}
