using System.Text.Json;
using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Data;

public static class CatalogSeeder
{
    private static readonly string[] HotelImages = ["hotelimg1.jfif", "hotelimg2.jfif", "hotelimg3.jfif", "hotelimg4.jfif", "hotelimg5.jfif", "hotelimg6.jfif", "hotelimg7.jfif"];
    private static readonly string[] TourImages = ["hotelimg1.jfif", "hotelimg2.jfif", "hotelimg3.jfif", "hotelimg4.jfif", "hotelimg5.jfif", "hotelimg6.jfif", "hotelimg7.jfif"];

    public static async Task MigrateAndSeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();
        await SeedAsync(context);
    }

    public static async Task SeedAsync(AppDbContext context)
    {
        // Don't try to clear old data - just add new if not exists
        // This prevents Foreign Key conflicts with existing Bookings/Reservations
        // Use the SQL script to manually clear old data if needed
        
        await SeedHotelsAsync(context);
        await SeedFlightsAsync(context);
        await SeedToursAsync(context);
        await LocationSeeder.SeedAsync(context);
        await SeedSiteContentAsync(context);
        await SeedDiscountsAsync(context);
        await SeedCouponAsync(context);
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
        // Check if we already have new hotels - prevent duplicates
        var existingHotelCount = await context.Hotels.CountAsync();
        if (existingHotelCount > 0)
        {
            // Already seeded, skip to avoid duplicates
            return;
        }

        var random = new Random(42); // Fixed seed for consistency
        var hotels = new List<Hotel>
        {
            // ========== AZƏRBAYCAN ==========
            // Bakı
            Hotel("AEHotel Baku Palace", "Bakı", "Azərbaycan", "İçərişəhər", "İçərişəhər, Qoşa Qala 12", 5, 4.9m, 542, "Tarixi İçərişəhərdə lüks otel - Qız qalası və tarixi abidələr yaxınlığında", 380m, "Lüks", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Flame Towers", "Bakı", "Azərbaycan", "Alov qüllələri", "Alov qüllələri kompleksi, Məhəmməd Hadi 1A", 5, 4.8m, 687, "Bakının simvolu - Alov qüllələrində Xəzər mənzərəli premium otaq", 420m, "Lüks", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Boulevard", "Bakı", "Azərbaycan", "Dənizkənarı bulvar", "Dənizkənarı milli park 25", 4, 4.7m, 398, "Dənizkənarı bulvar üzərində ailə oteli - Xəzər dənizi mənzərəsi", 290m, "Ailə", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Old City Boutique", "Bakı", "Azərbaycan", "Qız qalası", "İçərişəhər, Qız Qalası küçəsi 8", 4, 4.6m, 256, "Tarixi memarlıq və müasir rahatlıq - Qız qalası 2 dəqiqəlik məsafədə", 320m, "Boutique", GetRandomImage(HotelImages, random)),

            // Qəbələ
            Hotel("AEHotel Qabala Resort", "Qəbələ", "Azərbaycan", "Tufandağ", "Tufandağ Dağ-Xizək Kompleksi", 5, 4.8m, 467, "Tufandağ dağlarında lüks resort - qış və yay istirahəti üçün ideal", 350m, "Resort", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Nohur Lake", "Qəbələ", "Azərbaycan", "Nohur gölü", "Nohur gölü sahili 3", 5, 4.7m, 382, "Nohur gölü kənarında təbiət oteli - gölə baxan otaqlar", 340m, "Təbiət", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Seven Beauties", "Qəbələ", "Azərbaycan", "Yeddi gözəl şəlaləsi", "Vandam kəndi yolu 12", 4, 4.6m, 298, "Yeddi gözəl şəlaləsinə 10 dəqiqəlik yol - dağ mənzərəli otaqlar", 280m, "Təbiət", GetRandomImage(HotelImages, random)),

            // Şəki
            Hotel("AEHotel Sheki Palace", "Şəki", "Azərbaycan", "Şəki Xan Sarayı", "Şəki Xan Sarayı küçəsi 5", 5, 4.7m, 345, "Tarixi Xan Sarayına 1 dəqiqəlik məsafədə - milli memarlıq üslubunda", 310m, "Boutique", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Karvansaray", "Şəki", "Azərbaycan", "Karvansaray", "Karvansaray meydanı 1", 4, 4.6m, 267, "Tarixi İpək Yolu Karvansarayı yaxınlığında - ənənəvi qonaqpərvərlik", 270m, "Tarixi", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Kish Temple View", "Şəki", "Azərbaycan", "Kiş məbədi", "Kiş kəndi 8", 4, 4.5m, 198, "Qafqazın ən qədim kilsəsi - Kiş məbədinə 5 dəqiqəlik məsafədə", 240m, "Tarixi", GetRandomImage(HotelImages, random)),

            // ========== TÜRKİYƏ ==========
            // İstanbul
            Hotel("AEHotel Grand Istanbul", "İstanbul", "Türkiyə", "Ayasofya", "Sultanahmet Meydanı 15", 5, 4.9m, 982, "Ayasofya və Sultanahmet məscidi arasında - Osmanlı lüksü", 420m, "Lüks", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Topkapi Palace View", "İstanbul", "Türkiyə", "Topqapı sarayı", "Topkapı Sarayı yolu 8", 5, 4.8m, 756, "Topqapı Sarayına baxan otaqlar - Sultanahmet təpəsində", 390m, "Tarixi", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Galata Tower", "İstanbul", "Türkiyə", "Qalata qülləsi", "Galata Meydanı 22", 4, 4.7m, 543, "Qalata qülləsi yaxınlığında boutique otel - Boğaz mənzərəsi", 340m, "Boutique", GetRandomImage(HotelImages, random)),

            // Antalya
            Hotel("AEHotel Duden Resort", "Antalya", "Türkiyə", "Düden şəlaləsi", "Düden Şelale Caddesi 50", 5, 4.8m, 892, "Düden şəlaləsi mənzərəli resort - ailələr üçün ideal", 370m, "Resort", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Konyaalti Beach", "Antalya", "Türkiyə", "Konyaaltı", "Konyaaltı Sahili 120", 5, 4.7m, 687, "Konyaaltı çimərliyi üzərində - Antalya körfəzi mənzərəsi", 360m, "Çimərlik", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Kaleici Boutique", "Antalya", "Türkiyə", "Kaleiçi", "Kaleiçi Limanı 8", 4, 4.6m, 456, "Tarixi Kaleiçi limanında butik otel - köhnə şəhər atmosferi", 300m, "Boutique", GetRandomImage(HotelImages, random)),

            // Kapadokya
            Hotel("AEHotel Cave Luxury", "Kapadokya", "Türkiyə", "Göreme", "Göreme Kasabası Merkez 12", 5, 4.9m, 782, "Mağara otelində lüks qalma - hava şarı uçuşu mənzərəsi", 480m, "Mağara", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Pasabag Valley", "Kapadokya", "Türkiyə", "Paşabağ vadisi", "Paşabağ Yolu 15", 5, 4.8m, 543, "Paşabağ vadisi panoraması - pəri bacaları mənzərəsi", 450m, "Boutique", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Uchisar Castle", "Kapadokya", "Türkiyə", "Uçhisar qalası", "Uçhisar Kasabası 5", 4, 4.7m, 398, "Uçhisar qalası ətəyində - Kapadokya panoraması", 380m, "Təbiət", GetRandomImage(HotelImages, random)),

            // ========== BƏƏ ==========
            // Dubay
            Hotel("AEHotel Burj Khalifa View", "Dubay", "BƏƏ", "Burj Khalifa", "Downtown Dubai 88", 5, 4.9m, 1247, "Burj Khalifa mənzərəli - dünyanın ən hündür binası yanında", 580m, "Lüks", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Palm Jumeirah", "Dubay", "BƏƏ", "Palm Jumeirah", "Palm West Beach, Nakheel 12", 5, 4.9m, 1098, "Palm adası üzərində villa resort - şəxsi çimərlik", 620m, "Villa", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Dubai Marina", "Dubay", "BƏƏ", "Dubai Marina", "Marina Walk 45", 5, 4.8m, 856, "Dubai Marina mənzərəli premium otaq - yat limanı yaxınlığında", 520m, "Lüks", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Desert Safari Lodge", "Dubay", "BƏƏ", "Desert Safari", "Al Qudra Road, Desert Camp", 4, 4.7m, 542, "Səhra kempində ənənəvi təcrübə - Desert Safari daxildir", 450m, "Kamp", GetRandomImage(HotelImages, random)),

            // Əbu-Dabi
            Hotel("AEHotel Sheikh Zayed Grand", "Əbu-Dabi", "BƏƏ", "Şeyx Zayed məscidi", "Sheikh Rashid Bin Saeed Street 25", 5, 4.9m, 892, "Şeyx Zayed məscidinə 5 dəqiqəlik məsafədə - ağ mərmər memarlıq", 510m, "Lüks", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Louvre Abu Dhabi", "Əbu-Dabi", "BƏƏ", "Louvre Abu Dhabi", "Saadiyat Island Cultural District", 5, 4.8m, 687, "Louvre muzeyi yaxınlığında - mədəniyyət adası", 480m, "Mədəni", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Ferrari World", "Əbu-Dabi", "BƏƏ", "Ferrari World", "Yas Island 10", 5, 4.7m, 756, "Ferrari World və Yas Marina yaxınlığında - F1 həyəcanı", 460m, "Əyləncə", GetRandomImage(HotelImages, random)),

            // ========== FRANSA ==========
            // Paris
            Hotel("AEHotel Eiffel Tower View", "Paris", "Fransa", "Eyfel qülləsi", "Avenue de la Bourdonnais 52", 5, 4.9m, 1124, "Eyfel qülləsinə baxan otaqlar - Paris romantikası", 590m, "Lüks", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Louvre Palace", "Paris", "Fransa", "Luvr muzeyi", "Rue de Rivoli 120", 5, 4.9m, 982, "Luvr muzeyinə 2 dəqiqəlik məsafədə - tarixi saray oteli", 610m, "Tarixi", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Versailles Grand", "Paris", "Fransa", "Versal sarayı", "Place d'Armes, Versailles", 5, 4.8m, 756, "Versal sarayı bağlarına baxan otel - krallıq lüksü", 550m, "Saray", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Disneyland Resort", "Paris", "Fransa", "Disneyland Paris", "Disneyland Paris Marne-la-Vallée", 4, 4.7m, 1342, "Disneyland park girişi yaxınlığında - ailə hoteli", 380m, "Ailə", GetRandomImage(HotelImages, random)),

            // Nitsa
            Hotel("AEHotel Nice Riviera", "Nitsa", "Fransa", "Promenade des Anglais", "Promenade des Anglais 107", 5, 4.8m, 687, "Promenade des Anglais üzərində - Azur sahili mənzərəsi", 470m, "Sahil", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Castle Hill", "Nitsa", "Fransa", "Castle Hill", "Colline du Château, Mont Boron", 4, 4.7m, 456, "Castle Hill təpəsində panorama mənzərə - Old Town yaxınlığında", 420m, "Boutique", GetRandomImage(HotelImages, random)),

            // ========== İTALİYA ==========
            // Roma
            Hotel("AEHotel Colosseum View", "Roma", "İtaliya", "Kolizey", "Via del Colosseo 25", 5, 4.9m, 1024, "Kolizeya baxan otaqlar - qədim Roma tarixi", 520m, "Tarixi", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Trevi Fountain", "Roma", "İtaliya", "Trevi fəvvarəsi", "Via delle Muratte 15", 5, 4.8m, 856, "Trevi fəvvarəsinə 1 dəqiqəlik məsafədə - barokko memarlıq", 490m, "Boutique", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Vatican Palace", "Roma", "İtaliya", "Vatikan", "Via della Conciliazione 42", 5, 4.8m, 724, "Vatikan muzeyi və Müqəddəs Pyotr Bazilikası yaxınlığında", 510m, "Dini", GetRandomImage(HotelImages, random)),

            // Milan
            Hotel("AEHotel Duomo Grand", "Milan", "İtaliya", "Duomo Katedrali", "Piazza del Duomo 8", 5, 4.9m, 892, "Duomo meydanına baxan otaqlar - Milano qəlbi", 530m, "Lüks", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Sforza Castle", "Milan", "İtaliya", "Sforza qalası", "Piazza Castello 3", 5, 4.8m, 687, "Sforza qalası yaxınlığında - Renaissance memarlığı", 480m, "Tarixi", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Navigli Canal", "Milan", "İtaliya", "Navigli kanalları", "Naviglio Grande 22", 4, 4.7m, 542, "Navigli kanalları sahilində boutique otel - incəsənət məhəlləsi", 420m, "Boutique", GetRandomImage(HotelImages, random)),

            // ========== MALDİV ADALARI ==========
            // Malé
            Hotel("AEHotel Maldives Grand", "Malé", "Maldiv adaları", "Sultan Park", "Boduthakurufaanu Magu 50", 5, 4.9m, 756, "Sultan Park və National Museum yaxınlığında - paytaxt oteli", 620m, "Şəhər", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Maldives Beach", "Malé", "Maldiv adaları", "Artificial Beach", "Marine Drive Artificial Beach", 5, 4.8m, 624, "Artificial Beach-də su idmanları və snorkeling", 580m, "Çimərlik", GetRandomImage(HotelImages, random)),

            // Maafushi
            Hotel("AEHotel Maafushi Resort", "Maafushi", "Maldiv adaları", "Bikini Beach", "Maafushi Island North", 5, 4.9m, 542, "Bikini Beach və delfin turları - ada həyatı", 680m, "Resort", GetRandomImage(HotelImages, random)),
            Hotel("AEHotel Maafushi Dive", "Maafushi", "Maldiv adaları", "Ada ekskursiyası", "Maafushi Island South", 5, 4.8m, 467, "Snorkeling və ada ekskursiyaları - su idmanları mərkəzi", 650m, "Dalğıc", GetRandomImage(HotelImages, random)),

            // Vaadhoo
            Hotel("AEHotel Vaadhoo Glow", "Vaadhoo adası", "Maldiv adaları", "Parlayan çimərlik", "Vaadhoo Island", 5, 4.9m, 387, "Bioluminescent plaj - gecə parlayan dəniz", 780m, "Ekzotik", GetRandomImage(HotelImages, random))
        };

        // Add rooms to each hotel
        foreach (var hotel in hotels)
        {
            hotel.Rooms = new List<Room>
            {
                Room(hotel, "Standart Otaq", "Rahat və müasir otaq", 2, "1 King yataq", "30 m²", hotel.PricePerNight, GetRandomImage(HotelImages, random), ["Wi-Fi", "Smart TV", "Kondisioner", "Minibar"]),
                Room(hotel, "Deluxe Otaq", "Geniş və lüks otaq", 3, "1 King yataq + divan", "45 m²", hotel.PricePerNight + 80, GetRandomImage(HotelImages, random), ["Wi-Fi", "Smart TV", "Minibar", "Balkon", "Pəncərə mənzərəsi"]),
                Room(hotel, "Suite", "Premium suite - panorama mənzərə", 4, "2 King yataq", "65 m²", hotel.PricePerNight + 180, GetRandomImage(HotelImages, random), ["Wi-Fi", "Smart TV", "Spa", "Balkon", "Minibar", "Qonaq otağı"])
            };
        }

        context.Hotels.AddRange(hotels);
        await context.SaveChangesAsync();
    }

    private static async Task SeedFlightsAsync(AppDbContext context)
    {
        // Check if we already have flights - prevent duplicates
        var existingFlightCount = await context.Flights.CountAsync();
        if (existingFlightCount > 0)
        {
            return; // Already seeded
        }

        var flightTemplates = new (string Airline, string Number, string FromCode, string FromCity, string FromCountry, string ToCode, string ToCity, string ToCountry, int DepHour, int DepMinute, int ArrHour, int ArrMinute, string Duration, int Stops, decimal Price, string Cabin, int Seats, bool Refundable)[]
        {
            // Azərbaycan uçuşları
            ("AZAL", "J2-101", "GYD", "Bakı", "Azərbaycan", "IST", "İstanbul", "Türkiyə", 8, 30, 11, 15, "2s 45dəq", 0, 180m, "economy", 20, true),
            ("Turkish Airlines", "TK-252", "IST", "İstanbul", "Türkiyə", "GYD", "Bakı", "Azərbaycan", 14, 20, 17, 5, "2s 45dəq", 0, 190m, "economy", 18, true),
            ("AZAL", "J2-102", "GYD", "Bakı", "Azərbaycan", "AYT", "Antalya", "Türkiyə", 10, 0, 12, 30, "2s 30dəq", 0, 210m, "economy", 22, true),
            ("AZAL", "J2-201", "GYD", "Bakı", "Azərbaycan", "DXB", "Dubay", "BƏƏ", 22, 0, 2, 15, "3s 15dəq", 0, 320m, "economy", 16, true),
            ("Emirates", "EK-005", "DXB", "Dubay", "BƏƏ", "GYD", "Bakı", "Azərbaycan", 3, 30, 6, 45, "3s 15dəq", 0, 340m, "economy", 15, true),
            ("AZAL", "J2-301", "GYD", "Bakı", "Azərbaycan", "CDG", "Paris", "Fransa", 15, 0, 19, 15, "5s 15dəq", 0, 450m, "business", 12, true),
            ("Air France", "AF-655", "CDG", "Paris", "Fransa", "GYD", "Bakı", "Azərbaycan", 10, 30, 18, 45, "5s 15dəq", 0, 470m, "business", 14, true),

            // Türkiyə daxili və beynəlxalq
            ("Turkish Airlines", "TK-100", "IST", "İstanbul", "Türkiyə", "AYT", "Antalya", "Türkiyə", 7, 0, 8, 15, "1s 15dəq", 0, 120m, "economy", 30, false),
            ("Turkish Airlines", "TK-120", "IST", "İstanbul", "Türkiyə", "NAV", "Kapadokya", "Türkiyə", 9, 30, 10, 45, "1s 15dəq", 0, 130m, "economy", 25, false),
            ("Turkish Airlines", "TK-401", "IST", "İstanbul", "Türkiyə", "DXB", "Dubay", "BƏƏ", 1, 0, 5, 30, "4s 30dəq", 0, 380m, "economy", 18, true),
            ("Turkish Airlines", "TK-501", "IST", "İstanbul", "Türkiyə", "CDG", "Paris", "Fransa", 11, 0, 14, 15, "3s 15dəq", 0, 290m, "economy", 20, true),
            ("Turkish Airlines", "TK-601", "IST", "İstanbul", "Türkiyə", "FCO", "Roma", "İtaliya", 13, 30, 15, 0, "2s 30dəq", 0, 250m, "economy", 22, true),

            // BƏƏ uçuşları
            ("Emirates", "EK-123", "DXB", "Dubay", "BƏƏ", "AUH", "Əbu-Dabi", "BƏƏ", 8, 0, 9, 0, "1s", 0, 80m, "economy", 35, false),
            ("Emirates", "EK-201", "DXB", "Dubay", "BƏƏ", "CDG", "Paris", "Fransa", 2, 30, 7, 45, "7s 15dəq", 0, 520m, "business", 10, true),
            ("Emirates", "EK-301", "DXB", "Dubay", "BƏƏ", "MLE", "Malé", "Maldiv adaları", 9, 0, 14, 0, "4s", 0, 480m, "economy", 20, true),
            ("Maldivian", "Q2-501", "MLE", "Malé", "Maldiv adaları", "DXB", "Dubay", "BƏƏ", 15, 30, 19, 30, "4s", 0, 490m, "economy", 18, true),

            // Avropa uçuşları
            ("Air France", "AF-100", "CDG", "Paris", "Fransa", "NCE", "Nitsa", "Fransa", 7, 0, 8, 30, "1s 30dəq", 0, 150m, "economy", 30, false),
            ("Air France", "AF-201", "CDG", "Paris", "Fransa", "FCO", "Roma", "İtaliya", 9, 0, 11, 15, "2s 15dəq", 0, 180m, "economy", 25, true),
            ("Air France", "AF-301", "CDG", "Paris", "Fransa", "MXP", "Milan", "İtaliya", 11, 30, 13, 30, "2s", 0, 170m, "economy", 28, true),
            ("Alitalia", "AZ-101", "FCO", "Roma", "İtaliya", "MXP", "Milan", "İtaliya", 8, 0, 9, 15, "1s 15dəq", 0, 130m, "economy", 32, false)
        };

        foreach (var day in new[] { 5, 10, 15, 20, 25 })
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
        // Check if we already have tours - prevent duplicates
        var existingTourCount = await context.Tours.CountAsync();
        if (existingTourCount > 0)
        {
            return; // Already seeded
        }

        var random = new Random(42);
        context.Tours.AddRange(
            // ========== AZƏRBAYCAN TURLARI ==========
            Tour("Bakı Tarixi Tur", "Bakı, Azərbaycan", 4.9m, 542, "1 gün", 150m, GetRandomImage(TourImages, random), 
                "İçərişəhər, Qız qalası, Alov qüllələri və Dənizkənarı bulvar gəzintisi", "Tarixi", "Asan", 1, 25,
                ["İçərişəhər", "Qız qalası", "Alov qüllələri", "Dənizkənarı bulvar"],
                ["Bələdçi", "Nəqliyyat", "Giriş biletləri"],
                ["Yemək"],
                [("Tarixi Bakı", new[] { "İçərişəhər", "Qız qalası" }), ("Müasir Bakı", new[] { "Alov qüllələri", "Dənizkənarı bulvar" })],
                ["2026-11-05", "2026-11-10", "2026-11-15", "2026-11-20"]),

            Tour("Qəbələ Təbiət Macərası", "Qəbələ, Azərbaycan", 4.8m, 398, "2 gün / 1 gecə", 280m, GetRandomImage(TourImages, random),
                "Tufandağ dağ-xizək kurotu, Nohur gölü və Yeddi gözəl şəlaləsi", "Təbiət", "Orta", 2, 15,
                ["Tufandağ", "Nohur gölü", "Yeddi gözəl şəlaləsi"],
                ["Otel", "Səhər yeməyi", "Bələdçi", "Nəqliyyat"],
                ["Nahar", "Şam yeməyi"],
                [("Dağ turları", new[] { "Tufandağ", "Yeddi gözəl şəlaləsi" }), ("Göl gəzintisi", new[] { "Nohur gölü" })],
                ["2026-11-08", "2026-11-15", "2026-11-22"]),

            Tour("Şəki Mədəniyyət Turu", "Şəki, Azərbaycan", 4.7m, 312, "2 gün / 1 gecə", 240m, GetRandomImage(TourImages, random),
                "Şəki Xan Sarayı, Karvansaray və Kiş məbədi tarixi turlar", "Mədəniyyət", "Asan", 1, 20,
                ["Şəki Xan Sarayı", "Karvansaray", "Kiş məbədi", "Şəki bazarı"],
                ["Otel", "Bələdçi", "Giriş biletləri", "Nəqliyyat"],
                ["Yemək"],
                [("Tarixi abidələr", new[] { "Xan Sarayı", "Karvansaray", "Kiş məbədi" })],
                ["2026-11-12", "2026-11-18", "2026-11-25"]),

            // ========== TÜRKİYƏ TURLARI ==========
            Tour("İstanbul İki Qitə Turu", "İstanbul, Türkiyə", 4.9m, 892, "2 gün / 1 gecə", 220m, GetRandomImage(TourImages, random),
                "Ayasofya, Topqapı Sarayı, Qalata Qülləsi və Boğaz kruizi", "Tarixi", "Asan", 2, 30,
                ["Ayasofya", "Topqapı sarayı", "Qalata qülləsi", "Boğaz kruizi"],
                ["Bələdçi", "Giriş biletləri", "Nəqliyyat", "Boğaz kruizi"],
                ["Otel", "Yemək"],
                [("Tarixi yarımada", new[] { "Ayasofya", "Topqapı" }), ("Boğaz", new[] { "Qalata qülləsi", "Kruiz" })],
                ["2026-11-10", "2026-11-15", "2026-11-20", "2026-11-25"]),

            Tour("Antalya Təbiət Macərası", "Antalya, Türkiyə", 4.7m, 687, "3 gün / 2 gecə", 340m, GetRandomImage(TourImages, random),
                "Düden şəlaləsi, Konyaaltı və Kaleiçi qədim limanı", "Təbiət", "Asan", 2, 25,
                ["Düden şəlaləsi", "Konyaaltı", "Kaleiçi", "Qədim liman"],
                ["Otel", "Səhər yeməyi", "Bələdçi", "Nəqliyyat"],
                ["Nahar", "Şam yeməyi"],
                [("Şəlalə", new[] { "Düden şəlaləsi" }), ("Çimərlik", new[] { "Konyaaltı" }), ("Tarixi", new[] { "Kaleiçi" })],
                ["2026-11-05", "2026-11-12", "2026-11-19", "2026-11-26"]),

            Tour("Kapadokya Hava Şarı", "Kapadokya, Türkiyə", 4.9m, 1024, "2 gün / 1 gecə", 520m, GetRandomImage(TourImages, random),
                "Hava şarı uçuşu, Göreme, Paşabağ vadisi və Uçhisar qalası", "Macəra", "Orta", 2, 12,
                ["Hava şarı uçuşu", "Göreme", "Paşabağ vadisi", "Uçhisar qalası"],
                ["Hava şarı", "Otel", "Bələdçi", "Nəqliyyat", "Giriş biletləri"],
                ["Yemək"],
                [("Hava macərası", new[] { "Hava şarı uçuşu" }), ("Vadilər", new[] { "Göreme", "Paşabağ", "Uçhisar" })],
                ["2026-11-08", "2026-11-15", "2026-11-22"]),

            // ========== BƏƏ TURLARI ==========
            Tour("Dubay Lüks Şəhər Turu", "Dubay, BƏƏ", 4.9m, 1247, "3 gün / 2 gecə", 580m, GetRandomImage(TourImages, random),
                "Burj Khalifa, Palm Jumeirah, Dubai Marina və Desert Safari", "Şəhər", "Asan", 2, 20,
                ["Burj Khalifa", "Palm Jumeirah", "Dubai Marina", "Desert Safari"],
                ["Otel", "Bələdçi", "Nəqliyyat", "Desert Safari", "Giriş biletləri"],
                ["Yemək"],
                [("Şəhər turları", new[] { "Burj Khalifa", "Palm Jumeirah", "Dubai Marina" }), ("Səhra", new[] { "Desert Safari" })],
                ["2026-11-10", "2026-11-17", "2026-11-24"]),

            Tour("Əbu-Dabi Mədəniyyət Turu", "Əbu-Dabi, BƏƏ", 4.8m, 892, "2 gün / 1 gecə", 420m, GetRandomImage(TourImages, random),
                "Şeyx Zayed məscidi, Louvre Abu Dhabi və Ferrari World", "Mədəniyyət", "Asan", 2, 18,
                ["Şeyx Zayed məscidi", "Louvre Abu Dhabi", "Ferrari World"],
                ["Bələdçi", "Nəqliyyat", "Giriş biletləri"],
                ["Otel", "Yemək"],
                [("Mədəni", new[] { "Şeyx Zayed məscidi", "Louvre" }), ("Əyləncə", new[] { "Ferrari World" })],
                ["2026-11-12", "2026-11-19", "2026-11-26"]),

            // ========== FRANSA TURLARI ==========
            Tour("Paris Romantik Tur", "Paris, Fransa", 4.9m, 1456, "3 gün / 2 gecə", 520m, GetRandomImage(TourImages, random),
                "Eyfel qülləsi, Luvr muzeyi, Versal sarayı və Disneyland", "Romantik", "Asan", 2, 20,
                ["Eyfel qülləsi", "Luvr muzeyi", "Versal sarayı", "Disneyland Paris"],
                ["Bələdçi", "Nəqliyyat", "Giriş biletləri"],
                ["Otel", "Yemək"],
                [("Paris mərkəzi", new[] { "Eyfel qülləsi", "Luvr muzeyi" }), ("Kənar", new[] { "Versal sarayı", "Disneyland" })],
                ["2026-11-08", "2026-11-15", "2026-11-22"]),

            Tour("Nitsa French Riviera", "Nitsa, Fransa", 4.8m, 687, "2 gün / 1 gecə", 380m, GetRandomImage(TourImages, random),
                "Promenade des Anglais, Castle Hill və Old Town gəzintisi", "Sahil", "Asan", 2, 18,
                ["Promenade des Anglais", "Castle Hill", "Old Town"],
                ["Bələdçi", "Nəqliyyat"],
                ["Otel", "Yemək"],
                [("Sahil", new[] { "Promenade des Anglais" }), ("Tarixi", new[] { "Castle Hill", "Old Town" })],
                ["2026-11-10", "2026-11-17", "2026-11-24"]),

            // ========== İTALİYA TURLARI ==========
            Tour("Roma Qədim İmperiya", "Roma, İtaliya", 4.9m, 1124, "3 gün / 2 gecə", 480m, GetRandomImage(TourImages, random),
                "Kolizey, Trevi fəvvarəsi və Vatikan muzeyi", "Tarixi", "Asan", 2, 20,
                ["Kolizey", "Trevi fəvvarəsi", "Vatikan", "Forum Romanum"],
                ["Bələdçi", "Nəqliyyat", "Giriş biletləri"],
                ["Otel", "Yemək"],
                [("Qədim Roma", new[] { "Kolizey", "Forum" }), ("Vatikan", new[] { "Vatikan muzeyi" }), ("Barokko", new[] { "Trevi fəvvarəsi" })],
                ["2026-11-05", "2026-11-12", "2026-11-19"]),

            Tour("Milan Moda Paytaxtı", "Milan, İtaliya", 4.8m, 824, "2 gün / 1 gecə", 420m, GetRandomImage(TourImages, random),
                "Duomo Katedrali, Sforza qalası və Navigli kanalları", "Moda", "Asan", 2, 18,
                ["Duomo Katedrali", "Sforza qalası", "Navigli kanalları", "Moda küçələri"],
                ["Bələdçi", "Nəqliyyat", "Giriş biletləri"],
                ["Otel", "Yemək"],
                [("Tarixi", new[] { "Duomo", "Sforza qalası" }), ("Moda", new[] { "Moda küçələri" }), ("Kanallar", new[] { "Navigli" })],
                ["2026-11-08", "2026-11-15", "2026-11-22"]),

            // ========== MALDİV ADALARI TURLARI ==========
            Tour("Maldiv Tropik Cənnət", "Malé, Maldiv adaları", 4.9m, 824, "5 gün / 4 gecə", 1250m, GetRandomImage(TourImages, random),
                "Sultan Park, National Museum, Artificial Beach və snorkeling", "Plaj", "Asan", 2, 12,
                ["Sultan Park", "National Museum", "Artificial Beach", "Snorkeling", "Ada turları"],
                ["Resort", "Bütün yeməklər", "Snorkeling", "Ada turları"],
                [],
                [("Şəhər", new[] { "Sultan Park", "Museum" }), ("Plaj", new[] { "Artificial Beach", "Snorkeling" })],
                ["2026-11-10", "2026-11-20", "2026-11-28"]),

            Tour("Maafushi Ada Macərası", "Maafushi, Maldiv adaları", 4.8m, 642, "4 gün / 3 gecə", 980m, GetRandomImage(TourImages, random),
                "Bikini Beach, delfin turu və ada ekskursiyası", "Macəra", "Orta", 2, 10,
                ["Bikini Beach", "Delfin turu", "Ada ekskursiyası", "Snorkeling"],
                ["Resort", "Səhər yeməyi", "Delfin turu", "Snorkeling avadanlığı"],
                ["Nahar", "Şam yeməyi"],
                [("Su macəraları", new[] { "Snorkeling", "Delfin turu" }), ("Plaj", new[] { "Bikini Beach", "Ada ekskursiyası" })],
                ["2026-11-08", "2026-11-15", "2026-11-25"]),

            Tour("Vaadhoo Parlayan Plaj", "Vaadhoo adası, Maldiv adaları", 4.9m, 542, "3 gün / 2 gecə", 1150m, GetRandomImage(TourImages, random),
                "Bioluminescent plaj - gecə parlayan dəniz mənzərəsi", "Ekzotik", "Asan", 2, 8,
                ["Parlayan çimərlik", "Gecə dəniz mənzərəsi", "Snorkeling", "Romantik şam yeməyi"],
                ["Resort", "Bütün yeməklər", "Gecə turu"],
                [],
                [("Gecə macərası", new[] { "Bioluminescent plaj", "Gecə dəniz mənzərəsi" })],
                ["2026-11-12", "2026-11-22"])
        );

        await context.SaveChangesAsync();
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
                HeroTitleBefore = "Dünyanın ən yaxşı",
                HeroTitleAccent = "otelləri",
                HeroTitleAfter = "və turları kəşf edin",
                HeroDescription = "Azərbaycandan Avropaya, BƏƏ-dən Maldivlərə — premium otellər, rahat uçuşlar və unudulmaz turlar bir platformada",
                HeroPrimaryButtonText = "İndi kəşf et",
                HeroPrimaryButtonUrl = "#featured",
                HeroSecondaryButtonText = "Hekayəmizi izlə",
                HeroVideoUrl = "/uploads/aehotel-promo.mp4",
                HeroFloatImage1Url = "/uploads/hotelimg6.jfif",
                HeroFloatImage2Url = "/uploads/hotelimg7.jfif",
                HeroSupportBadge = "7/24",
                HeroSupportLabel = "Müştəri dəstəyi",
                HeroStoryTitle = "AE Hotel hekayəsi",
                HeroStoryText = "2018-dən bəri minlərlə səyahətçini dünyanın dörd bir yanındakı lüks otellərlə birləşdiririk",
                AdvantagesTitle = "Niyə AE Hotel?",
                AdvantagesDescription = "40+ otel, 20+ tur və 100+ uçuş seçimi",
                AdvantagesImageUrl = "/uploads/hotelimg2.jfif",
                AdvantagesRating = "4.8",
                GalleryTitle = "AE Hotel dünyası",
                FeaturedEyebrow = "Seçilmiş kolleksiya",
                FeaturedTitle = "Ən məşhur otellər",
                FooterDescription = "AE Hotel-də lüks sadəcə ünvan deyil — hiss, təcrübə və həyat tərzidir",
                FooterLinksTitle = "Keçidlər",
                FooterHoursTitle = "Dəstək saatları",
                FooterContactTitle = "Əlaqə",
                HoursWeekdays = "Bazar ertəsi – Cümə",
                HoursSaturday = "Şənbə",
                HoursSunday = "Bazar",
                HoursValue = "24 saat",
                ContactAddress = "Bakı, Azərbaycan, 28 May küçəsi 5",
                ContactEmail = "info@aehotel.com",
                ContactPhone = "+994 55 123 45 67",
                CopyrightText = "© 2026 AE Hotel. Bütün hüquqlar qorunur.",
                PrivacyUrl = "/privacy",
                TermsUrl = "/terms",
                LinkedInUrl = "https://linkedin.com/company/aehotel",
                TwitterUrl = "https://twitter.com/aehotel",
                InstagramUrl = "https://instagram.com/aehotel",
                FacebookUrl = "https://facebook.com/aehotel",
                NoticeSummer = "Yay endirimi — otellərdə 20%-dək",
                NoticeSupport = "7/24 dəstək aktivdir",
                HotelsPageTitle = "Otelləri Kəşf Et",
                HotelsPageSubtitle = "Dünya üzrə premium otellər",
                HotelsPageBannerUrl = "/uploads/hotels-hero.jpg",
                HotelsPageVideoUrl = "",
                FlightsPageTitle = "Uçuşları Kəşf Et",
                FlightsPageSubtitle = "Ən yaxşı qiymətlərlə uçuşlar",
                FlightsPageBannerUrl = "/uploads/flights-hero.jpg",
                FlightsPageVideoUrl = "",
                ToursPageTitle = "Turları Kəşf Et",
                ToursPageSubtitle = "Unudulmaz səyahət təcrübələri",
                ToursPageBannerUrl = "/uploads/tours-hero.jpg",
                ToursPageVideoUrl = ""
            });
        }

        if (!await context.SiteMenuItems.AnyAsync())
        {
            context.SiteMenuItems.AddRange(
                new SiteMenuItem { Label = "Otellər", Url = "/hotels", Placement = "discover", IsRoute = true, SortOrder = 1, IsActive = true },
                new SiteMenuItem { Label = "Uçuşlar", Url = "/flights", Placement = "discover", IsRoute = true, SortOrder = 2, IsActive = true },
                new SiteMenuItem { Label = "Turlar", Url = "/tours", Placement = "discover", IsRoute = true, SortOrder = 3, IsActive = true },
                new SiteMenuItem { Label = "Haqqımızda", Url = "/about", Placement = "pages", IsRoute = true, SortOrder = 1, IsActive = true },
                new SiteMenuItem { Label = "Əlaqə", Url = "/contact", Placement = "pages", IsRoute = true, SortOrder = 2, IsActive = true }
            );
        }

        if (!await context.SiteAdvantages.AnyAsync())
        {
            context.SiteAdvantages.AddRange(
                new SiteAdvantage { IconKey = "globe", Color = "#4ade80", Title = "40+ otel", Description = "6 ölkədə premium otellər", SortOrder = 1, IsActive = true },
                new SiteAdvantage { IconKey = "shield", Color = "#3b82f6", Title = "Təhlükəsiz", Description = "SSL şifrələmə", SortOrder = 2, IsActive = true },
                new SiteAdvantage { IconKey = "clock", Color = "#f43f5e", Title = "7/24 dəstək", Description = "Həmişə yanınızdayıq", SortOrder = 3, IsActive = true },
                new SiteAdvantage { IconKey = "star", Color = "#fb923c", Title = "4.8/5", Description = "Yüksək reytinq", SortOrder = 4, IsActive = true }
            );
        }

        if (!await context.SiteSlides.AnyAsync())
        {
            context.SiteSlides.AddRange(
                new SiteSlide { ImageUrl = "/uploads/hotelimg1.jfif", Title = "Bakı", SortOrder = 1, IsActive = true },
                new SiteSlide { ImageUrl = "/uploads/hotelimg2.jfif", Title = "İstanbul", SortOrder = 2, IsActive = true },
                new SiteSlide { ImageUrl = "/uploads/hotelimg3.jfif", Title = "Dubay", SortOrder = 3, IsActive = true },
                new SiteSlide { ImageUrl = "/uploads/hotelimg4.jfif", Title = "Paris", SortOrder = 4, IsActive = true },
                new SiteSlide { ImageUrl = "/uploads/hotelimg5.jfif", Title = "Maldiv", SortOrder = 5, IsActive = true }
            );
        }

        await context.SaveChangesAsync();
    }

    private static Hotel Hotel(string title, string city, string country, string location, string address, int stars, decimal rating, int reviews, string description, decimal price, string type, string image)
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
            AmenitiesJson = JsonSerializer.Serialize(new[] { "Wi-Fi", "Restoran", "Hovuz", "Spa" })
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

    private static string GetRandomImage(string[] images, Random random)
    {
        return images[random.Next(images.Length)];
    }
}
