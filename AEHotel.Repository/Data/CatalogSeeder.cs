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
        await SeedHotelsAsync(context);
        await SeedFlightsAsync(context);
        await SeedToursAsync(context);
        await EnsureCrossSellAsync(context);
        await SeedSiteContentAsync(context);
        await SeedDiscountsAsync(context);
        await SeedCouponAsync(context);
        await SeedApprovedReviewsAsync(context);
    }

    private static async Task SeedDiscountsAsync(AppDbContext context)
    {
        if (await context.DiscountPackages.AnyAsync())
        {
            return;
        }

        context.DiscountPackages.AddRange(
            new DiscountPackage { Name = "Otel + uçuş", Combo = "hotel_flight", Percent = 5, IsActive = true },
            new DiscountPackage { Name = "Otel + tur", Combo = "hotel_tour", Percent = 7, IsActive = true },
            new DiscountPackage { Name = "Otel + uçuş + tur", Combo = "hotel_flight_tour", Percent = 10, IsActive = true }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedCouponAsync(AppDbContext context)
    {
        if (await context.DiscountPackages.AnyAsync(item => item.Code == "SAVE10"))
        {
            return;
        }

        context.DiscountPackages.Add(new DiscountPackage
        {
            Name = "Kupon 10%",
            Combo = "coupon",
            Code = "SAVE10",
            Percent = 10,
            IsActive = true
        });
        await context.SaveChangesAsync();
    }

    private static async Task SeedHotelsAsync(AppDbContext context)
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
        await context.SaveChangesAsync();
    }

    private static async Task SeedFlightsAsync(AppDbContext context)
    {
        if (await context.Flights.AnyAsync())
        {
            return;
        }

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

        await context.SaveChangesAsync();
    }

    private static async Task SeedToursAsync(AppDbContext context)
    {
        if (await context.Tours.AnyAsync())
        {
            return;
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

    private static async Task EnsureCrossSellAsync(AppDbContext context)
    {
        if (!await context.Hotels.AnyAsync(hotel => hotel.City == "Bakı"))
        {
            var baku = Hotel("Fairmont Bakı Flame Towers", "Bakı", "Azərbaycan", "Flame Towers", "Mehdi Hüseyn küçəsi 1A", 5, 4.8m, 964, "Şəhər mərkəzində Flame Towers yanında otel.", 260m, "Şəhər", "hotelimg1.jfif", ["Wi-Fi", "Hovuz", "Restoran"]);
            baku.Rooms =
            [
                Room(baku, "City View", "Şəhər mənzərəli otaq.", 2, "1 King yataq", "34 m2", 260m, "hotelimg1.jfif", ["Wi-Fi", "Smart TV"]),
                Room(baku, "Family Room", "Ailə otağı.", 4, "1 King + 2 twin", "46 m2", 340m, "hotelimg5.jfif", ["Wi-Fi", "Ailə"])
            ];
            context.Hotels.Add(baku);
        }

        if (!await context.Flights.AnyAsync(flight => flight.FlightNumber == "J2-781"))
        {
            foreach (var day in new[] { 10, 15, 20, 25, 28 })
            {
                context.Flights.Add(Flight("AZAL", "J2-781", "GYD", "Bakı", "Azərbaycan", "AYT", "Antalya", "Türkiyə", day, 8, 40, 11, 5, "2s 25dəq", 0, 195m, "economy", 18, true));
                context.Flights.Add(Flight("AZAL", "J2-782", "AYT", "Antalya", "Türkiyə", "GYD", "Bakı", "Azərbaycan", day, 18, 10, 20, 35, "2s 25dəq", 0, 195m, "economy", 18, true));
            }
        }

        if (!await context.Tours.AnyAsync(tour => tour.Name == "Antalya Sahil Turu"))
        {
            context.Tours.Add(Tour("Antalya Sahil Turu", "Antalya, Türkiyə", 4.6m, 188, "3 gün / 2 gecə", 720m, "hotelimg4.jfif", "Lara sahili və şəhər mərkəzi marşrutu.", "Sahil", "Asan", 1, 20,
                ["Lara sahili", "Kaleiçi"],
                ["Otel", "Transfer", "Bələdçi"],
                ["Uçuş", "Şəxsi xərclər"],
                [("Sahil günü", new[] { "Lara plajı", "Kaleiçi" })],
                ["2026-11-10", "2026-11-15", "2026-11-20", "2026-11-25"]));
        }

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

    private static async Task SeedSiteContentAsync(AppDbContext context)
    {
        if (!await context.SiteSettings.AnyAsync())
        {
            context.SiteSettings.Add(new SiteSettings
            {
                SiteName = "AEHotel",
                LogoUrl = "/uploads/AE_HOTEL_transparent_logo.png",
                HeroKicker = "Otel & Travel · AE Hotel",
                HeroTitleBefore = "Yaxınlıqdakı ən yaxşı",
                HeroTitleAccent = "otelləri",
                HeroTitleAfter = "və turları tapın.",
                HeroDescription = "Yalnız yer yox — büdcənizə uyğun lüks təcrübə, uçuş və turu bir yerdə bron edin.",
                HeroPrimaryButtonText = "İndi kəşf et",
                HeroPrimaryButtonUrl = "#featured",
                HeroSecondaryButtonText = "Hekayəmizi izlə",
                HeroVideoUrl = "/uploads/aehotel-promo.mp4",
                HeroFloatImage1Url = "/uploads/hotelimg6.jfif",
                HeroFloatImage2Url = "/uploads/hotelimg7.jfif",
                HeroSupportBadge = "7/24",
                HeroSupportLabel = "Müştəri dəstəyi",
                HeroStoryTitle = "AE Hotel hekayəsi",
                HeroStoryText = "2018-dən bəri minlərlə səyahətçini lüks otellərlə, şəhər turları və wellness təcrübələri ilə birləşdiririk. Hər rezervasiya — şəxsi konsultasiya kimidir.",
                AdvantagesTitle = "Ən yaxşı tətil buradan başlayır",
                AdvantagesDescription = "Oteli bron edin, eyni anda uçuş və tur əlavə edin — kampaniya kodları ilə qalmanızı daha sərfəli edin.",
                AdvantagesImageUrl = "/uploads/hotelimg2.jfif",
                AdvantagesRating = "4.8",
                GalleryTitle = "AE Hotel dünyası",
                FeaturedEyebrow = "Seçilmiş kolleksiya",
                FeaturedTitle = "Önə çıxan otellər",
                FooterDescription = "AE Hotel-də lüks sadəcə ünvan deyil — hiss, təcrübə və həyat tərzidir. Otel, uçuş və tur bir platformada.",
                FooterLinksTitle = "Keçidlər",
                FooterHoursTitle = "Dəstək saatları",
                FooterContactTitle = "Əlaqə",
                HoursWeekdays = "Bazar ertəsi – Cümə",
                HoursSaturday = "Şənbə",
                HoursSunday = "Bazar",
                HoursValue = "24 saat",
                ContactAddress = "UK, 1212, 192/8 New Elephant Road London",
                ContactEmail = "info@aehotel.com",
                ContactPhone = "(094) 542 - 4789",
                CopyrightText = "© 2026 AE Hotel. All rights reserved.",
                PrivacyUrl = "#",
                TermsUrl = "#",
                LinkedInUrl = "#",
                TwitterUrl = "#",
                InstagramUrl = "#",
                FacebookUrl = "#",
                NoticeSummer = "Yay endirimi — otellərdə 20%-dək",
                NoticeSupport = "7/24 dəstək aktivdir"
            });
        }

        if (!await context.SiteMenuItems.AnyAsync())
        {
            context.SiteMenuItems.AddRange(
                new SiteMenuItem { Label = "Otellər", Url = "/hotels", Placement = "discover", IsRoute = true, SortOrder = 1, IsActive = true },
                new SiteMenuItem { Label = "Təyinatlar", Url = "/destinations", Placement = "discover", IsRoute = true, SortOrder = 2, IsActive = true },
                new SiteMenuItem { Label = "Turlar", Url = "/tours", Placement = "discover", IsRoute = true, SortOrder = 3, IsActive = true },
                new SiteMenuItem { Label = "Haqqımızda", Url = "/about", Placement = "pages", IsRoute = true, SortOrder = 1, IsActive = true },
                new SiteMenuItem { Label = "Rəylər", Url = "/reviews", Placement = "pages", IsRoute = true, SortOrder = 2, IsActive = true },
                new SiteMenuItem { Label = "Qalereya", Url = "/gallery", Placement = "pages", IsRoute = true, SortOrder = 3, IsActive = true },
                new SiteMenuItem { Label = "Otel", Url = "/hotels", Placement = "quick", IconKey = "hotel", IsRoute = true, SortOrder = 1, IsActive = true },
                new SiteMenuItem { Label = "Uçuş", Url = "/flights", Placement = "quick", IconKey = "plane", IsRoute = true, SortOrder = 2, IsActive = true },
                new SiteMenuItem { Label = "Tur", Url = "/tours", Placement = "quick", IconKey = "car", IsRoute = true, SortOrder = 3, IsActive = true },
                new SiteMenuItem { Label = "Ana səhifə", Url = "/", Placement = "footer", IsRoute = true, SortOrder = 1, IsActive = true },
                new SiteMenuItem { Label = "Haqqımızda", Url = "/about", Placement = "footer", IsRoute = true, SortOrder = 2, IsActive = true },
                new SiteMenuItem { Label = "Otellər", Url = "/hotels", Placement = "footer", IsRoute = true, SortOrder = 3, IsActive = true },
                new SiteMenuItem { Label = "Turlar", Url = "/tours", Placement = "footer", IsRoute = true, SortOrder = 4, IsActive = true }
            );
        }

        if (!await context.SiteAdvantages.AnyAsync())
        {
            context.SiteAdvantages.AddRange(
                new SiteAdvantage { IconKey = "utensils", Color = "#4ade80", Title = "Keyfiyyətli yemək", Description = "Yerli və beynəlxalq mətbəx — chef menyusu və halal seçimlər.", SortOrder = 1, IsActive = true },
                new SiteAdvantage { IconKey = "bolt", Color = "#f43f5e", Title = "Sürətli xidmət", Description = "Dəqiqəlik check-in, ani təsdiq və 24 saat otaq xidməti.", SortOrder = 2, IsActive = true },
                new SiteAdvantage { IconKey = "shield", Color = "#fb923c", Title = "Təhlükəsiz ödəniş", Description = "SSL bron, geri qaytarılan tariflər və sığorta paketləri.", SortOrder = 3, IsActive = true },
                new SiteAdvantage { IconKey = "clock", Color = "#3b82f6", Title = "7/24 dəstək", Description = "Səyahət boyu canlı dəstək.", SortOrder = 4, IsActive = true }
            );
        }

        var taxiMenus = await context.SiteMenuItems
            .Where(item => item.Url == "/taxis" || item.IconKey == "taxi")
            .ToListAsync();
        if (taxiMenus.Count > 0)
        {
            context.SiteMenuItems.RemoveRange(taxiMenus);
        }

        var settings = await context.SiteSettings.FirstOrDefaultAsync();
        if (settings != null)
        {
            if (settings.FooterDescription.Contains("taksi", StringComparison.OrdinalIgnoreCase))
            {
                settings.FooterDescription = "AE Hotel-də lüks sadəcə ünvan deyil — hiss, təcrübə və həyat tərzidir. Otel, uçuş və tur bir platformada.";
            }

            if (settings.HeroDescription.Contains("transfer", StringComparison.OrdinalIgnoreCase))
            {
                settings.HeroDescription = "Yalnız yer yox — büdcənizə uyğun lüks təcrübə, uçuş və turu bir yerdə bron edin.";
            }

            if (settings.AdvantagesDescription.Contains("transfer", StringComparison.OrdinalIgnoreCase))
            {
                settings.AdvantagesDescription = "Oteli bron edin, eyni anda uçuş və tur əlavə edin — kampaniya kodları ilə qalmanızı daha sərfəli edin.";
            }
        }

        if (!await context.SiteSlides.AnyAsync())
        {
            context.SiteSlides.AddRange(
                new SiteSlide { ImageUrl = "/uploads/hotelimg1.jfif", Title = "Gecə şəhər mənzərəsi", SortOrder = 1, IsActive = true },
                new SiteSlide { ImageUrl = "/uploads/hotelimg2.jfif", Title = "Otel girişi", SortOrder = 2, IsActive = true },
                new SiteSlide { ImageUrl = "/uploads/hotelimg4.jfif", Title = "Dənizə baxan otaq", SortOrder = 3, IsActive = true },
                new SiteSlide { ImageUrl = "/uploads/hotelimg6.jfif", Title = "Fəvvarə və işıqlar", SortOrder = 4, IsActive = true },
                new SiteSlide { ImageUrl = "/uploads/hotelimg3.jfif", Title = "Sahil yataq otağı", SortOrder = 5, IsActive = true },
                new SiteSlide { ImageUrl = "/uploads/hotelimg5.jfif", Title = "Dam hovuzu", SortOrder = 6, IsActive = true },
                new SiteSlide { ImageUrl = "/uploads/hotelimg7.jfif", Title = "Şəhər suitı", SortOrder = 7, IsActive = true }
            );
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedApprovedReviewsAsync(AppDbContext context)
    {
        var hotels = await context.Hotels.AsNoTracking().OrderBy(item => item.Id).ToListAsync();
        if (hotels.Count == 0)
        {
            return;
        }

        var existing = await context.StayReviews.AsNoTracking()
            .Select(item => new { item.HotelId, item.GuestName })
            .ToListAsync();
        var seen = existing
            .Select(item => $"{item.HotelId}|{item.GuestName}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = new List<StayReview>();
        foreach (var hotel in hotels)
        {
            foreach (var sample in DisplayReviewsFor(hotel))
            {
                var key = $"{hotel.Id}|{sample.GuestName}";
                if (!seen.Add(key))
                {
                    continue;
                }

                toAdd.Add(new StayReview
                {
                    HotelId = hotel.Id,
                    BookingId = null,
                    UserId = "catalog-seed",
                    GuestName = sample.GuestName,
                    Rating = sample.Rating,
                    Title = sample.Title,
                    Comment = sample.Comment,
                    IsApproved = true,
                    CreatedAt = sample.CreatedAt
                });
            }
        }

        if (toAdd.Count == 0)
        {
            return;
        }

        context.StayReviews.AddRange(toAdd);
        await context.SaveChangesAsync();
    }

    private static IReadOnlyList<(string GuestName, int Rating, string Title, string Comment, DateTime CreatedAt)> DisplayReviewsFor(Hotel hotel)
    {
        var title = hotel.Title ?? string.Empty;
        if (title.Contains("Oasis", StringComparison.OrdinalIgnoreCase))
        {
            return OasisReviews;
        }

        if (title.Contains("Çendu", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("Cendu", StringComparison.OrdinalIgnoreCase))
        {
            return CenduReviews;
        }

        return [];
    }

    private static readonly (string GuestName, int Rating, string Title, string Comment, DateTime CreatedAt)[] CenduReviews =
    [
        ("Leyla Əliyeva", 5, "Möhtəşəm təcrübə!", "Oteldə qaldığımız müddətdə hər şey mükəmməl idi. Otaqlar çox təmiz, xidmət əla, məkan gözəl. Ailə ilə gəlmişdik və hamı çox razı qaldı. Səhər yeməyi çox zəngin idi, dəniz mənzərəsi əlavə bonus.", DateTime.SpecifyKind(new DateTime(2024, 8, 20), DateTimeKind.Utc)),
        ("Rəşad Məmmədov", 4, "Çox gözəl otel", "Ümumiyyətlə çox bəyəndik. Otaqlar müasir və rahat. WiFi sürəti yaxşı idi, bu bizim üçün vacib idi. Hovuz sahəsi gözəl dizayn olunub. Restoran yeməkləri dadlı idi.", DateTime.SpecifyKind(new DateTime(2024, 7, 25), DateTimeKind.Utc)),
        ("Aysel Həsənova", 5, "Ailə üçün ideal", "Uşaqlarla gəlmişdik və hər şey əla idi. Uşaq hovuzu təhlükəsiz və əyləncəli. Animatorlar çox mehriban. Otaqlarda uşaqlar üçün əlavə yataq və ləvazimatlar hazırlamışdılar.", DateTime.SpecifyKind(new DateTime(2024, 6, 15), DateTimeKind.Utc)),
        ("Cavid Quliyev", 4, "İş səfəri üçün rahat", "İş səfərində qaldım. Otaq rahat idi, iş masası və yaxşı işıqlandırma var. Resepsiyada çox kömək oldular. Yeganə problem parkda yer tapmaq çətinliyi idi.", DateTime.SpecifyKind(new DateTime(2024, 5, 20), DateTimeKind.Utc))
    ];

    private static readonly (string GuestName, int Rating, string Title, string Comment, DateTime CreatedAt)[] OasisReviews =
    [
        ("Nigar Əhmədova", 5, "Lüks və rahatlıq", "Balayı səfərimiz üçün bu oteli seçdik və heç peşman olmadıq. Hər şey lüks və rahat. SPA mərkəzi möhtəşəm idi. Romantik şam yeməyi xidməti unutulmaz oldu.", DateTime.SpecifyKind(new DateTime(2024, 8, 5), DateTimeKind.Utc)),
        ("Elvin Sultanov", 4, "Yaxşı xidmət", "Qonaqpərvərlik səviyyəsi yüksək. Personal çox diqqətli. Otaqlar geniş. Bazarla yaxın məsafədə olması çox əlverişli.", DateTime.SpecifyKind(new DateTime(2024, 7, 14), DateTimeKind.Utc))
    ];

}
