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

    private static readonly (string City, string[] Places)[] PlaceCatalog =
    [
        ("Bakı", ["İçərişəhər", "Alov qüllələri", "Dənizkənarı bulvar", "Qız qalası", "Heydər Əliyev Mərkəzi"]),
        ("Qəbələ", ["Tufandağ", "Nohur gölü", "Yeddi gözəl şəlaləsi"]),
        ("Şəki", ["Şəki Xan Sarayı", "Karvansaray", "Kiş məbədi"]),
        ("Gəncə", ["Nizami Gəncəvi məqbərəsi", "Göygöl"]),
        ("İstanbul", ["Ayasofya", "Topqapı sarayı", "Qalata qülləsi", "Boğaz"]),
        ("Antalya", ["Düden şəlaləsi", "Konyaaltı", "Kaleiçi"]),
        ("Kapadokya", ["Göreme", "Paşabağ vadisi", "Uçhisar qalası"]),
        ("Dubay", ["Burj Xəlifə", "Dubay Mall", "Palm Jumeirah"]),
        ("Əbu-Dabi", ["Şeyx Zayed məscidi", "Yas adası"]),
        ("Paris", ["Eyfel qülləsi", "Luvr", "Monmartr"]),
        ("Nitsa", ["Promenade des Anglais", "Köhnə Nitsa"]),
        ("Roma", ["Kolizey", "Vatikan", "Trevi fontanı"]),
        ("Milan", ["Duomo", "Galleria Vittorio Emanuele II"]),
        ("London", ["Big Ben", "London Eye", "Tower Bridge"]),
        ("Berlin", ["Brandenburq qapısı", "Muzey adası"]),
        ("Barselona", ["Sagrada Familia", "Park Güell"]),
        ("Madrid", ["Prado muzeyi", "Retiro parkı"]),
        ("Tbilisi", ["Köhnə Tbilisi", "Narikala qalası"]),
        ("Batumi", ["Batumi bulvarı", "Alphabetic Tower"]),
        ("Bangkok", ["Böyük Saray", "Wat Arun"]),
        ("Phuket", ["Patong çimərliyi", "Phi Phi adaları"]),
        ("Malé", ["Hulhumalé çimərliyi", "Malé balıq bazarı"]),
        ("Maafushi", ["Maafushi çimərliyi", "Bikini çimərliyi"])
    ];

    public static async Task SeedAsync(AppDbContext context)
    {
        var countries = await context.Countries
            .Include(item => item.Cities)
            .ThenInclude(city => city.TouristPlaces)
            .ToListAsync();

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

        foreach (var (cityName, places) in PlaceCatalog)
        {
            foreach (var city in countries.SelectMany(item => item.Cities)
                         .Where(item => string.Equals(item.Name, cityName, StringComparison.OrdinalIgnoreCase)))
            {
                foreach (var place in places)
                {
                    EnsurePlace(city, place);
                }
            }
        }

        // Hotels already store their tourist place as text (Location); turn it into a row so they can be linked.
        var hotels = await context.Hotels.Where(item => item.CityId == null || item.TouristPlaceId == null).ToListAsync();
        foreach (var hotel in hotels)
        {
            var city = countries
                .Where(item => string.Equals(item.Name, hotel.Country, StringComparison.OrdinalIgnoreCase))
                .SelectMany(item => item.Cities)
                .FirstOrDefault(item => string.Equals(item.Name, hotel.City, StringComparison.OrdinalIgnoreCase));
            if (city != null && !string.IsNullOrWhiteSpace(hotel.Location))
            {
                EnsurePlace(city, hotel.Location.Trim());
            }
        }

        await context.SaveChangesAsync();

        foreach (var hotel in hotels)
        {
            var city = countries
                .Where(item => string.Equals(item.Name, hotel.Country, StringComparison.OrdinalIgnoreCase))
                .SelectMany(item => item.Cities)
                .FirstOrDefault(item => string.Equals(item.Name, hotel.City, StringComparison.OrdinalIgnoreCase));
            if (city == null)
            {
                continue;
            }

            hotel.CityId = city.Id;
            hotel.TouristPlaceId = city.TouristPlaces
                .FirstOrDefault(item => string.Equals(item.Name, hotel.Location?.Trim(), StringComparison.OrdinalIgnoreCase))?.Id;
        }

        await context.SaveChangesAsync();
    }

    private static void EnsurePlace(City city, string name)
    {
        if (!city.TouristPlaces.Any(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            city.TouristPlaces.Add(new TouristPlace { Name = name });
        }
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
