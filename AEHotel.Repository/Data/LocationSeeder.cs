using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Data;

/// <summary>
/// Fills the Countries and Cities tables: a base catalog plus every country/city pair already used by
/// hotels, flights and tours, so existing records always resolve to a row.
/// </summary>
public static class LocationSeeder
{
    private static readonly (string Name, string Iso, string[] Cities)[] Catalog =
    [
        ("Azərbaycan", "AZ", ["Bakı", "Qəbələ", "Şəki", "Gəncə", "Lənkəran", "Şuşa", "Naxçıvan", "Sumqayıt"]),
        ("Türkiyə", "TR", ["İstanbul", "Antalya", "Kapadokya", "Ankara", "İzmir", "Bodrum", "Trabzon"]),
        ("BƏƏ", "AE", ["Dubay", "Əbu-Dabi", "Şarcə"]),
        ("İtaliya", "IT", ["Roma", "Milan", "Venesiya", "Florensiya", "Neapol"]),
        ("Fransa", "FR", ["Paris", "Nitsa", "Lion", "Marsel"]),
        ("Almaniya", "DE", ["Berlin", "Münhen", "Frankfurt", "Hamburq"]),
        ("Böyük Britaniya", "GB", ["London", "Mançester", "Edinburq"]),
        ("İspaniya", "ES", ["Madrid", "Barselona", "Valensiya", "Sevilya"]),
        ("Gürcüstan", "GE", ["Tbilisi", "Batumi", "Kutaisi"]),
        ("Tayland", "TH", ["Bangkok", "Phuket", "Pattaya", "Çiang Mai"]),
        ("Maldiv adaları", "MV", ["Malé", "Maafushi"]),
        ("Rusiya", "RU", ["Moskva", "Sankt-Peterburq"]),
        ("ABŞ", "US", ["Nyu-York", "Los-Anceles", "Mayami"]),
        ("Yaponiya", "JP", ["Tokio", "Kioto", "Osaka"])
    ];

    public static async Task SeedAsync(AppDbContext context)
    {
        var countries = await context.Countries.Include(item => item.Cities).ToListAsync();

        foreach (var (name, iso, cities) in Catalog)
        {
            var country = Ensure(context, countries, name, iso);
            foreach (var city in cities)
            {
                EnsureCity(country, city);
            }
        }

        foreach (var hotel in await context.Hotels.AsNoTracking().Select(item => new { item.City, item.Country }).ToListAsync())
        {
            EnsurePair(context, countries, hotel.City, hotel.Country);
        }

        foreach (var flight in await context.Flights.AsNoTracking()
                     .Select(item => new { item.OriginCity, item.OriginCountry, item.DestinationCity, item.DestinationCountry })
                     .ToListAsync())
        {
            EnsurePair(context, countries, flight.OriginCity, flight.OriginCountry);
            EnsurePair(context, countries, flight.DestinationCity, flight.DestinationCountry);
        }

        foreach (var destination in await context.Tours.AsNoTracking().Select(item => item.Destination).ToListAsync())
        {
            var parts = destination.Split(',', 2, StringSplitOptions.TrimEntries);
            if (parts.Length > 1)
            {
                EnsurePair(context, countries, parts[0], parts[1]);
            }
        }

        await context.SaveChangesAsync();
    }

    private static void EnsurePair(AppDbContext context, List<Country> countries, string? city, string? country)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            return;
        }

        var row = Ensure(context, countries, country.Trim(), isoCode: null);
        if (!string.IsNullOrWhiteSpace(city))
        {
            EnsureCity(row, city.Trim());
        }
    }

    private static Country Ensure(AppDbContext context, List<Country> countries, string name, string? isoCode)
    {
        var country = countries.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        if (country != null)
        {
            if (string.IsNullOrWhiteSpace(country.IsoCode) && !string.IsNullOrWhiteSpace(isoCode))
            {
                country.IsoCode = isoCode;
            }

            return country;
        }

        country = new Country { Name = name, IsoCode = isoCode ?? string.Empty };
        countries.Add(country);
        context.Countries.Add(country);
        return country;
    }

    private static void EnsureCity(Country country, string name)
    {
        if (!country.Cities.Any(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            country.Cities.Add(new City { Name = name });
        }
    }
}
