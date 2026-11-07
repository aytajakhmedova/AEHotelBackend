using AEHotel.Domain.Entities;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.DTOs.Discounts;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Services.Interfaces;

namespace AEHotel.Service.Services;

public class DiscountService : IDiscountService
{
    public static readonly string[] Combos = ["hotel_flight", "hotel_tour", "hotel_flight_tour", "coupon"];

    // A guest pays hotel, flight and tour in one checkout, each as its own booking carrying the same coupon.
    // The first booking marks a personal coupon as used; the bookings of that same checkout may still use it.
    private static readonly TimeSpan SameCheckout = TimeSpan.FromMinutes(30);

    private readonly IDiscountPackageRepository _packages;

    public DiscountService(IDiscountPackageRepository packages)
    {
        _packages = packages;
    }

    public async Task<IReadOnlyList<DiscountPackageResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Personal coupons belong to their owners and are not part of the shared list.
        var items = await _packages.GetAllAsync(cancellationToken);
        return items.Where(item => item.UserId == null).Select(Map).ToList();
    }

    public async Task<IReadOnlyList<DiscountPackageResponse>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var items = await _packages.GetAllAsync(cancellationToken);
        return items
            .Where(item => item.UserId == null)
            .Where(item => item.IsActive)
            .Where(item => !item.StartsAt.HasValue || item.StartsAt <= now)
            .Where(item => !item.EndsAt.HasValue || item.EndsAt >= now)
            .Select(Map)
            .ToList();
    }

    public async Task<decimal> GetActivePercentAsync(string? combo, CancellationToken cancellationToken = default)
    {
        var key = NormalizeCombo(combo);
        if (key is null || key == "coupon")
        {
            return 0;
        }

        var package = await GetActiveByComboAsync(key, cancellationToken);
        return package?.Percent ?? 0;
    }

    public async Task<DiscountPackageResponse?> GetActiveByComboAsync(string? combo, CancellationToken cancellationToken = default)
    {
        var key = NormalizeCombo(combo);
        if (key is null || key == "coupon")
        {
            return null;
        }

        var package = await _packages.GetActiveByComboAsync(key, DateTime.UtcNow, cancellationToken);
        return package is null ? null : Map(package);
    }

    public async Task<DiscountPackageResponse?> GetActiveByCodeAsync(string? code, CancellationToken cancellationToken = default, string? userId = null)
    {
        var key = NormalizeCode(code);
        if (key is null)
        {
            return null;
        }

        var package = await _packages.GetActiveByCodeAsync(key, DateTime.UtcNow, cancellationToken);
        if (package?.UserId != null)
        {
            // A personal coupon works only for its owner, and only once.
            if (package.UserId != userId)
            {
                return null;
            }

            if (IsUsed(package))
            {
                throw new ValidationException("Bu kupon artıq istifadə olunub.");
            }
        }

        return package is null ? null : Map(package);
    }

    public async Task<DiscountPackageResponse> CreateWelcomeCouponAsync(string userId, decimal percent, int validMinutes, CancellationToken cancellationToken = default)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        string code;
        do
        {
            var suffix = new string(Enumerable.Range(0, 6)
                .Select(_ => alphabet[System.Security.Cryptography.RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray());
            code = "WELCOME" + (int)percent + suffix;
        }
        while (await _packages.GetByCodeAsync(code, tracking: false, cancellationToken) != null);

        var now = DateTime.UtcNow;
        var package = new DiscountPackage
        {
            Name = $"Xoş gəldin kuponu {(int)percent}%",
            Combo = "coupon",
            Code = code,
            Percent = percent,
            StartsAt = now,
            EndsAt = now.AddMinutes(validMinutes),
            IsActive = true,
            UserId = userId
        };
        await _packages.AddAsync(package, cancellationToken);
        await _packages.SaveChangesAsync(cancellationToken);
        return Map(package);
    }

    public async Task<IReadOnlyList<DiscountPackageResponse>> GetMineAsync(string userId, CancellationToken cancellationToken = default)
    {
        return (await _packages.GetAllAsync(cancellationToken))
            .Where(item => item.UserId == userId && item.IsActive)
            .OrderByDescending(item => item.Id)
            .Select(Map)
            .ToList();
    }

    public async Task ConsumeAsync(string? code, CancellationToken cancellationToken = default)
    {
        var key = NormalizeCode(code);
        if (key is null)
        {
            return;
        }

        var package = await _packages.GetByCodeAsync(key, tracking: true, cancellationToken);
        if (package?.UserId != null && package.UsedAt == null)
        {
            package.UsedAt = DateTime.UtcNow;
            await _packages.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool IsUsed(DiscountPackage package) =>
        package.UsedAt.HasValue && DateTime.UtcNow - package.UsedAt.Value > SameCheckout;

    public async Task<DiscountPackageResponse> CreateAsync(DiscountPackageRequest request, CancellationToken cancellationToken = default)
    {
        var package = new DiscountPackage();
        await ApplyAsync(package, request, excludeId: null, cancellationToken);
        await _packages.AddAsync(package, cancellationToken);
        await _packages.SaveChangesAsync(cancellationToken);
        return Map(package);
    }

    public async Task<DiscountPackageResponse> UpdateAsync(int id, DiscountPackageRequest request, CancellationToken cancellationToken = default)
    {
        var package = await _packages.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Discount package with id {id} was not found.");
        await ApplyAsync(package, request, id, cancellationToken);
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

    public static string? NormalizeCode(string? code)
    {
        var value = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (value.Length is < 3 or > 40)
        {
            return null;
        }

        return value.All(character => char.IsLetterOrDigit(character)) ? value : null;
    }

    private async Task ApplyAsync(DiscountPackage package, DiscountPackageRequest request, int? excludeId, CancellationToken cancellationToken)
    {
        if (request.Percent < 0 || request.Percent > 100)
        {
            throw new ValidationException("Percent must be between 0 and 100.");
        }

        if (request.Amount < 0)
        {
            throw new ValidationException("Amount cannot be negative.");
        }

        var code = string.IsNullOrWhiteSpace(request.Code) ? null : NormalizeCode(request.Code);
        if (!string.IsNullOrWhiteSpace(request.Code) && code is null)
        {
            throw new ValidationException("Kupon kodu 3–40 simvol, yalnız hərf və rəqəm ola bilər.");
        }

        string combo;
        if (code != null)
        {
            combo = "coupon";
            var existing = await _packages.GetByCodeAsync(code, tracking: false, cancellationToken);
            if (existing != null && existing.Id != excludeId)
            {
                throw new ValidationException("Bu kupon kodu artıq mövcuddur.");
            }
        }
        else
        {
            combo = NormalizeCombo(request.Combo)
                ?? throw new ValidationException("Combo must be hotel_flight, hotel_tour, hotel_flight_tour, or coupon.");
            if (combo == "coupon")
            {
                throw new ValidationException("Kupon üçün kod yazın.");
            }
        }

        package.Name = string.IsNullOrWhiteSpace(request.Name) ? (code ?? combo) : request.Name.Trim();
        package.Combo = combo;
        package.Code = code;
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
            Code = package.Code,
            Percent = package.Percent,
            Amount = package.Amount,
            StartsAt = package.StartsAt,
            EndsAt = package.EndsAt,
            IsActive = package.IsActive,
            UsedAt = package.UsedAt
        };
    }
}
