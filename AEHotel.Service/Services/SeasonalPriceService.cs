using AEHotel.Domain.Entities;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.DTOs.SeasonalPrices;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Services.Interfaces;

namespace AEHotel.Service.Services;

public class SeasonalPriceService : ISeasonalPriceService
{
    private static readonly string[] Currencies = ["AZN", "TRY", "EUR", "USD"];

    private readonly ISeasonalPriceRepository _prices;

    public SeasonalPriceService(ISeasonalPriceRepository prices)
    {
        _prices = prices;
    }

    public async Task<IReadOnlyList<SeasonalPriceResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return (await _prices.GetAllAsync(cancellationToken)).Select(Map).ToList();
    }

    public async Task<SeasonalPriceResponse> CreateAsync(SeasonalPriceRequest request, CancellationToken cancellationToken = default)
    {
        Normalize(request);
        Validate(request);
        if (await _prices.ExistsAsync(request.Country, request.Month, null, cancellationToken))
        {
            throw new ValidationException("A price range for this country and month already exists.");
        }

        var price = new SeasonalPrice();
        Apply(price, request);
        await _prices.AddAsync(price, cancellationToken);
        await _prices.SaveChangesAsync(cancellationToken);
        return Map(price);
    }

    public async Task<SeasonalPriceResponse> UpdateAsync(int id, SeasonalPriceRequest request, CancellationToken cancellationToken = default)
    {
        Normalize(request);
        Validate(request);
        var price = await _prices.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Seasonal price with id {id} was not found.");
        if (await _prices.ExistsAsync(request.Country, request.Month, id, cancellationToken))
        {
            throw new ValidationException("A price range for this country and month already exists.");
        }

        Apply(price, request);
        await _prices.SaveChangesAsync(cancellationToken);
        return Map(price);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var price = await _prices.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Seasonal price with id {id} was not found.");
        _prices.Remove(price);
        await _prices.SaveChangesAsync(cancellationToken);
    }

    private static void Normalize(SeasonalPriceRequest request)
    {
        request.Country = (request.Country ?? string.Empty).Trim();
        request.Currency = (request.Currency ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static void Validate(SeasonalPriceRequest request)
    {
        if (request.Country.Length is 0 or > 100)
        {
            throw new ValidationException("Country is required (up to 100 characters).");
        }

        if (request.Month is < 1 or > 12)
        {
            throw new ValidationException("Month must be between 1 and 12.");
        }

        if (!Currencies.Contains(request.Currency))
        {
            throw new ValidationException("Currency must be AZN, TRY, EUR or USD.");
        }

        if (request.MinPrice < 0 || request.MaxPrice < request.MinPrice)
        {
            throw new ValidationException("Prices must not be negative and the maximum must not be below the minimum.");
        }
    }

    private static void Apply(SeasonalPrice price, SeasonalPriceRequest request)
    {
        price.Country = request.Country;
        price.Month = request.Month;
        price.MinPrice = request.MinPrice;
        price.MaxPrice = request.MaxPrice;
        price.Currency = request.Currency;
    }

    private static SeasonalPriceResponse Map(SeasonalPrice price) => new()
    {
        Id = price.Id,
        Country = price.Country,
        Month = price.Month,
        MinPrice = price.MinPrice,
        MaxPrice = price.MaxPrice,
        Currency = price.Currency,
    };
}
