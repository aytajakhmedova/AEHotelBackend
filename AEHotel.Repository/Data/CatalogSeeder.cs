using System.Text.Json;
using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Data;

public static class CatalogSeeder
{
    public static async Task MigrateAndSeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();
        await SeedAsync(context);
    }

    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Hotels.AnyAsync())
        {
            return;
        }

        var hotels = new[]
        {
            Hotel("Çendu Corinthia", "Çendu", "Çin", "Gui Xi Şəhər Parkının yanı", "No. 88, Jinrong Avenue", 5, 4.8m, 1284, "Gui Xi Şəhər Parkının yanında ucalan lüks otel.", 420m, "Lüks", "hotelimg2.jfif", ["Spa", "Hovuz", "Wi-Fi", "Restoran"]),
            Hotel("Corinthia Oasis", "Malta", "Malta", "Aralıq dənizi sahili", "Valletta Waterfront", 5, 4.9m, 892, "Wellness və dəniz mənzərəsi bir ünvanda.", 510m, "Wellness", "hotelimg3.jfif", ["Wellness", "Plaj", "Spa", "Wi-Fi"]),
            Hotel("Paradise Beach Hotel", "Antalya", "Türkiyə", "Lara sahili", "Lara Caddesi 10", 5, 4.6m, 1750, "Ailələr üçün çimərlik oteli.", 280m, "Ailə", "hotelimg4.jfif", ["Ailə", "Hovuz", "Wi-Fi", "Restoran"])
        };

        hotels[0].Rooms =
        [
            Room(hotels[0], "Standart Otaq", "Rahat standart otaq.", 2, "1 King yataq", "32 m2", 420m, "hotelimg5.jfif", ["Wi-Fi", "Smart TV", "Kondisioner"]),
            Room(hotels[0], "Deluxe Room", "Şəhər mənzərəli geniş otaq.", 3, "1 King yataq + divan", "45 m2", 580m, "hotelimg1.jfif", ["Wi-Fi", "Minibar", "Balkon"])
        ];
        hotels[1].Rooms =
        [
            Room(hotels[1], "Dəniz Mənzərəli Otaq", "Dənizə baxan otaq.", 2, "1 King yataq", "36 m2", 510m, "hotelimg3.jfif", ["Wi-Fi", "Balkon"]),
            Room(hotels[1], "Wellness Suite", "Spa girişi olan suite.", 2, "1 King yataq", "52 m2", 740m, "hotelimg7.jfif", ["Spa", "Wi-Fi"])
        ];
        hotels[2].Rooms =
        [
            Room(hotels[2], "Family Room", "Ailə otağı.", 4, "1 King + 2 twin", "48 m2", 280m, "hotelimg4.jfif", ["Wi-Fi", "Ailə"]),
            Room(hotels[2], "Garden Room", "Bağ mənzərəli otaq.", 3, "2 Queen yataq", "40 m2", 320m, "hotelimg6.jfif", ["Wi-Fi", "Balkon"])
        ];

        context.Hotels.AddRange(hotels);

        var flightTemplates = new (string Airline, string Number, string FromCode, string FromCity, string FromCountry, string ToCode, string ToCity, string ToCountry, int DepHour, int DepMinute, int ArrHour, int ArrMinute, string Duration, int Stops, decimal Price, string Cabin, int Seats, bool Refundable)[]
        {
            ("AZAL", "J2-5620", "GYD", "Bakı", "Azərbaycan", "IST", "İstanbul", "Türkiyə", 14, 50, 17, 35, "2s 45dəq", 0, 180m, "economy", 15, true),
            ("Turkish Airlines", "TK-1254", "GYD", "Bakı", "Azərbaycan", "IST", "İstanbul", "Türkiyə", 9, 20, 12, 10, "2s 50dəq", 0, 220m, "economy", 8, false),
            ("AZAL", "J2-701", "GYD", "Bakı", "Azərbaycan", "DXB", "Dubay", "BƏƏ", 22, 10, 2, 5, "3s 55dəq", 0, 310m, "economy", 12, true),
            ("Emirates", "EK-124", "DXB", "Dubay", "BƏƏ", "LHR", "London", "Böyük Britaniya", 8, 15, 12, 40, "7s 25dəq", 0, 890m, "business", 6, true)
        };

        foreach (var day in new[] { 10, 15, 20, 25, 28 })
        {
            foreach (var item in flightTemplates)
            {
                context.Flights.Add(Flight(item.Airline, item.Number, item.FromCode, item.FromCity, item.FromCountry, item.ToCode, item.ToCity, item.ToCountry, day, item.DepHour, item.DepMinute, item.ArrHour, item.ArrMinute, item.Duration, item.Stops, item.Price, item.Cabin, item.Seats, item.Refundable));
            }
        }

        context.Tours.AddRange(
            Tour("Bakı Şəhər Turu", "Bakı, Azərbaycan", 4.9m, 342, "3 gün / 2 gecə", 850m, "hotelimg6.jfif", "İçərişəhərdən Flame Towers-ə qədər Bakı turu.", "Şəhər Turu", "Asan", 2, 15,
                ["İçərişəhər", "Flame Towers", "Bulvar"],
                ["Otel", "Səhər yeməyi", "Bələdçi"],
                ["Beynəlxalq uçuş", "Şəxsi xərclər"],
                [("Tarixi Bakı", new[] { "İçərişəhər", "Qız Qalası" }), ("Müasir Bakı", new[] { "Heydər Əliyev Mərkəzi", "Flame Towers" })],
                ["2026-11-01", "2026-11-15", "2026-12-05"]),
            Tour("Qəbələ Təbiət Turu", "Qəbələ, Azərbaycan", 4.8m, 218, "2 gün / 1 gecə", 620m, "hotelimg7.jfif", "Nohur gölü və Tufandağ marşrutu.", "Təbiət", "Orta", 2, 12,
                ["Nohur gölü", "Tufandağ"],
                ["Transfer", "Bələdçi"],
                ["Şəxsi xərclər"],
                [("Təbiət günü", new[] { "Nohur gölü", "Tufandağ" })],
                ["2026-11-08", "2026-11-22"]),
            Tour("Şəki Mədəniyyət Turu", "Şəki, Azərbaycan", 4.7m, 156, "2 gün / 1 gecə", 540m, "hotelimg1.jfif", "Xan sarayı və Şəki bazarı.", "Mədəniyyət", "Asan", 1, 16,
                ["Xan sarayı", "Kərvansaray"],
                ["Otel", "Giriş biletləri"],
                ["Nahar"],
                [("Şəki mərkəzi", new[] { "Xan sarayı", "Kərvansaray" })],
                ["2026-11-12", "2026-12-03"]));

        await context.SaveChangesAsync();
    }

    private static Hotel Hotel(string title, string city, string country, string location, string address, int stars, decimal rating, int reviews, string description, decimal price, string type, string image, string[] amenities)
    {
        return new Hotel
        {
            Title = title,
            City = city,
            Country = country,
            Location = location,
            Address = address,
            Stars = stars,
            Rating = rating,
            ReviewCount = reviews,
            Description = description,
            PricePerNight = price,
            HotelType = type,
            CheckInTime = "14:00",
            CheckOutTime = "12:00",
            ImageFile = image,
            AmenitiesJson = JsonSerializer.Serialize(amenities)
        };
    }

    private static Room Room(Hotel hotel, string name, string description, int capacity, string bed, string size, decimal price, string image, string[] amenities)
    {
        return new Room
        {
            Hotel = hotel,
            Name = name,
            Description = description,
            Capacity = capacity,
            BedType = bed,
            Size = size,
            PricePerNight = price,
            Available = true,
            ImageFile = image,
            AmenitiesJson = JsonSerializer.Serialize(amenities)
        };
    }

    private static Flight Flight(string airline, string number, string fromCode, string fromCity, string fromCountry, string toCode, string toCity, string toCountry, int day, int depHour, int depMinute, int arrHour, int arrMinute, string duration, int stops, decimal price, string cabin, int seats, bool refundable)
    {
        var departure = new DateTime(2026, 11, day, depHour, depMinute, 0);
        var arrival = new DateTime(2026, 11, day, arrHour, arrMinute, 0);
        if (arrival <= departure)
        {
            arrival = arrival.AddDays(1);
        }

        return new Flight
        {
            Airline = airline,
            FlightNumber = number,
            OriginCode = fromCode,
            OriginCity = fromCity,
            OriginCountry = fromCountry,
            DestinationCode = toCode,
            DestinationCity = toCity,
            DestinationCountry = toCountry,
            DepartureAt = departure,
            ArrivalAt = arrival,
            Duration = duration,
            Stops = stops,
            Price = price,
            CabinClass = cabin,
            SeatsAvailable = seats,
            Refundable = refundable
        };
    }

    private static Tour Tour(string name, string destination, decimal rating, int reviews, string duration, decimal price, string image, string description, string category, string difficulty, int minGuests, int maxGuests, string[] highlights, string[] included, string[] excluded, (string Title, string[] Activities)[] days, string[] dates)
    {
        var itinerary = days.Select((day, index) => new { day = index + 1, title = day.Title, activities = day.Activities }).ToList();
        return new Tour
        {
            Name = name,
            Destination = destination,
            Rating = rating,
            ReviewCount = reviews,
            Duration = duration,
            Price = price,
            ImageFile = image,
            Description = description,
            Category = category,
            Difficulty = difficulty,
            MinGuests = minGuests,
            MaxGuests = maxGuests,
            HighlightsJson = JsonSerializer.Serialize(highlights),
            IncludedJson = JsonSerializer.Serialize(included),
            ExcludedJson = JsonSerializer.Serialize(excluded),
            ItineraryJson = JsonSerializer.Serialize(itinerary),
            AvailableDatesJson = JsonSerializer.Serialize(dates)
        };
    }
}
