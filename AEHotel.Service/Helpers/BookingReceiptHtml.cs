using System.Globalization;
using AEHotel.Service.DTOs.Bookings;

namespace AEHotel.Service.Helpers;

public static class BookingReceiptHtml
{
    public static string Build(BookingResponse booking)
    {
        var total = booking.TotalPrice.ToString("0.00", CultureInfo.InvariantCulture);
        return $"""
            <html><body style="font-family:Arial,sans-serif;color:#1c1917">
            <h2>AE Hotel rezervasiya sənədi</h2>
            <p>Kod: <strong>{booking.Code}</strong></p>
            <p>Status: {booking.Status}</p>
            <p>Otel: {booking.HotelName}</p>
            <p>Otaq: {booking.RoomName}</p>
            <p>Tarixlər: {booking.CheckIn} — {booking.CheckOut}</p>
            <p>Qonaq sayı: {booking.Guests}</p>
            <p>Qiymət: ₼ {total}</p>
            <p>Ödəniş üsulu: {booking.PaymentMethod}</p>
            <p>Ödəniş statusu: {booking.PaymentStatus}</p>
            <p style="color:#8a8178;font-size:13px">Kart ödənişi demo rejimindədir. Real kart məlumatı saxlanılmır.</p>
            </body></html>
            """;
    }
}
