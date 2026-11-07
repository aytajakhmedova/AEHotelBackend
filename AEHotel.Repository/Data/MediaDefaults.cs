using System.Text.Json;
using System.Text.RegularExpressions;
using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Data;

/// <summary>
/// Gives hotels, tours, site settings and the gallery their pictures and videos in the database, so the
/// website does not have to carry any. It only fills what is empty (or still the old stock picture),
/// so anything an admin uploaded or changed is never overwritten. All files are in wwwroot/uploads.
/// </summary>
public static class MediaDefaults
{
    private static readonly Regex StockHotelImage = new(@"^hotelimg\d+\.(jfif|jpe?g|png)$", RegexOptions.IgnoreCase);

    private static readonly string[] BakuPhotos = ["bakuhotel.jpg", "bakuhotel1.jpg", "bakuhotel2.jpg", "bakuhotel3.jpg"];
    private static readonly string[] MaldivesPhotos = ["hotelmaldivler.jpg", "hotelmaldivler1.jpg", "hotelmaldivler2.jpg", "hotelmaldivler3.jpg"];
    private static readonly string[] GeneralPhotos = ["hotelyeni.jpg", "hotell.jpg", "hotelyeni1.jpg", "hotelll.jpg", "hotelyeni3.jpg", "hotelyeni4.jpg", "HOTEL1.jpg", "HOTEL2.jpg"];

    private static readonly Dictionary<string, string> TourPhotos = new()
    {
        ["Bakı Tarixi Tur"] = "baku-historical.jfif",
        ["Qəbələ Təbiət Macərası"] = "gabala-nature.jfif",
        ["Şəki Mədəniyyət Turu"] = "sheki-culture.jfif",
        ["İstanbul İki Qitə Turu"] = "istanbul-two-continents.jfif",
        ["Antalya Təbiət Macərası"] = "antalya-adventure.jfif",
        ["Kapadokya Hava Şarı"] = "cappadocia-balloon.jfif",
        ["Dubay Lüks Şəhər Turu"] = "dubai-luxury.jfif",
        ["Əbu-Dabi Mədəniyyət Turu"] = "abu-dhabi-culture.jfif",
        ["Paris Romantik Tur"] = "paris-romanti.jfif",
        ["Nitsa French Riviera"] = "nice-riviera.jfif",
        ["Roma Qədim İmperiya"] = "rome-ancient.jfif",
        ["Milan Moda Paytaxtı"] = "milan-fashion.jfif",
        ["Maldiv Tropik Cənnət"] = "maldives-paradise.jfif",
        ["Maafushi Ada Macərası"] = "maafushi-adventure.jfif",
        ["Vaadhoo Parlayan Plaj"] = "vaadhoo-beach.jfif",
    };

    private static readonly (string File, string Title)[] GalleryPhotos =
    [
        ("AEHOTEL.png", "AE Hotel girişi"),
        ("HOTEL1.jpg", "Kanal restoran"),
        ("HOTEL2.jpg", "Gecə hovuz"),
        ("TUR1.jpg", "Tur şəhəri"),
        ("TUR2.jpg", "Hava şarları"),
        ("VIDE01.png", "Dəniz turu"),
        ("VIDEOO.png", "Gecə turu"),
    ];

    public static async Task EnsureAsync(AppDbContext context)
    {
        await EnsureHotelPhotosAsync(context);
        await EnsureTourPhotosAsync(context);
        await EnsureSiteMediaAsync(context);
        await EnsureGalleryAsync(context);
    }

    private static async Task EnsureHotelPhotosAsync(AppDbContext context)
    {
        var hotels = await context.Hotels.ToListAsync();
        var changed = false;
        foreach (var hotel in hotels)
        {
            var hasGallery = !string.IsNullOrWhiteSpace(hotel.GalleryJson) && hotel.GalleryJson.Trim() != "[]";
            if (hasGallery)
            {
                continue;
            }

            var customMain = !string.IsNullOrWhiteSpace(hotel.ImageFile) && !StockHotelImage.IsMatch(hotel.ImageFile);
            var photos = PhotosFor(hotel);
            var gallery = (customMain ? new[] { hotel.ImageFile }.Concat(photos) : photos).Distinct().Take(4).ToArray();
            hotel.GalleryJson = JsonSerializer.Serialize(gallery);
            if (!customMain)
            {
                hotel.ImageFile = gallery[0];
            }

            changed = true;
        }

        if (changed)
        {
            await context.SaveChangesAsync();
        }
    }

    private static string[] PhotosFor(Hotel hotel)
    {
        if (string.Equals(hotel.Country, "Azərbaycan", StringComparison.OrdinalIgnoreCase))
        {
            return Rotate(BakuPhotos, hotel.Id);
        }

        if (hotel.Country.Contains("Maldiv", StringComparison.OrdinalIgnoreCase))
        {
            return Rotate(MaldivesPhotos, hotel.Id);
        }

        return Rotate(GeneralPhotos, hotel.Id);
    }

    private static string[] Rotate(string[] list, int seed)
    {
        var offset = Math.Abs(seed) % list.Length;
        return [.. list.Skip(offset), .. list.Take(offset)];
    }

    private static async Task EnsureTourPhotosAsync(AppDbContext context)
    {
        var tours = await context.Tours.ToListAsync();
        var changed = false;
        foreach (var tour in tours)
        {
            var stock = string.IsNullOrWhiteSpace(tour.ImageFile) || StockHotelImage.IsMatch(tour.ImageFile);
            if (stock && TourPhotos.TryGetValue(tour.Name, out var file))
            {
                tour.ImageFile = file;
                changed = true;
            }
        }

        if (changed)
        {
            await context.SaveChangesAsync();
        }
    }

    private static async Task EnsureSiteMediaAsync(AppDbContext context)
    {
        var settings = await context.SiteSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            return;
        }

        // Empty, or still the stock value the first seed wrote, becomes the intended picture or video.
        static string Pick(string current, string wanted, params string[] stock) =>
            string.IsNullOrWhiteSpace(current) || stock.Contains(current) ? wanted : current;

        settings.HeroVideoUrl = Pick(settings.HeroVideoUrl, "/uploads/aehotel-promo.mp4");
        settings.HeroFloatImage1Url = Pick(settings.HeroFloatImage1Url, "/uploads/HOTEL1.jpg", "/uploads/hotelimg6.jfif");
        settings.HeroFloatImage2Url = Pick(settings.HeroFloatImage2Url, "/uploads/TUR2.jpg", "/uploads/hotelimg7.jfif");
        settings.AdvantagesImageUrl = Pick(settings.AdvantagesImageUrl, "/uploads/AEHOTEL.png", "/uploads/hotelimg2.jfif");
        settings.HotelsPageVideoUrl = Pick(settings.HotelsPageVideoUrl, "/uploads/OTELVIDEOS.mp4");
        settings.FlightsPageVideoUrl = Pick(settings.FlightsPageVideoUrl, "/uploads/UCUS1.mp4");
        settings.ToursPageVideoUrl = Pick(settings.ToursPageVideoUrl, "/uploads/YENITURVIDEOS.mp4");
        await context.SaveChangesAsync();
    }

    private static async Task EnsureGalleryAsync(AppDbContext context)
    {
        // Only for a database that still has the first five slides: later changes by an admin are left alone.
        var slides = await context.SiteSlides.ToListAsync();
        if (slides.Count == 0 || slides.Count > 5)
        {
            return;
        }

        var order = slides.Max(slide => slide.SortOrder);
        foreach (var (file, title) in GalleryPhotos)
        {
            context.SiteSlides.Add(new SiteSlide { ImageUrl = $"/uploads/{file}", Title = title, SortOrder = ++order, IsActive = true });
        }

        await context.SaveChangesAsync();
    }
}
