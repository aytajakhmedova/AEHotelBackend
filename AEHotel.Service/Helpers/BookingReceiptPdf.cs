using System.Globalization;
using AEHotel.Service.DTOs.Bookings;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AEHotel.Service.Helpers;

public static class BookingReceiptPdf
{
    static BookingReceiptPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(BookingResponse booking)
    {
        var total = booking.TotalPrice.ToString("0.00", CultureInfo.InvariantCulture);
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(text => text.FontSize(12).FontFamily("Segoe UI"));
                page.Header().Text("AE Hotel rezervasiya sənədi").FontSize(20).SemiBold();
                page.Content().PaddingTop(16).Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Text($"Kod: {booking.Code}").SemiBold();
                    column.Item().Text($"Status: {booking.Status}");
                    column.Item().Text($"Otel: {booking.HotelName}");
                    column.Item().Text($"Otaq: {booking.RoomName}");
                    column.Item().Text($"Tarixlər: {booking.CheckIn} — {booking.CheckOut}");
                    column.Item().Text($"Qonaq sayı: {booking.Guests}");
                    column.Item().Text($"Qiymət: ₼ {total}");
                    column.Item().Text($"Ödəniş üsulu: {booking.PaymentMethod}");
                    column.Item().Text($"Ödəniş statusu: {booking.PaymentStatus}");
                    column.Item().PaddingTop(12)
                        .Text("Kart ödənişi demo rejimindədir. Real kart məlumatı saxlanılmır.")
                        .FontSize(10)
                        .FontColor("#8a8178");
                });
            });
        }).GeneratePdf();
    }
}
