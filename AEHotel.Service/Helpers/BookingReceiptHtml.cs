using System.Globalization;
using System.Net;
using AEHotel.Service.DTOs.Bookings;

namespace AEHotel.Service.Helpers;

public static class BookingReceiptHtml
{
    public static string Build(BookingResponse booking)
    {
        var listTotal = booking.TotalPrice + booking.DiscountAmount;
        var hotel = H(string.IsNullOrWhiteSpace(booking.HotelName) ? booking.ItemName : booking.HotelName);
        var discountRow = booking.DiscountAmount > 0
            ? Row(string.IsNullOrWhiteSpace(booking.CouponCode) ? "Endirim" : $"Kupon {H(booking.CouponCode)}", $"− {Money(booking.DiscountAmount)}")
            : "";
        var depositRows = booking.DepositPercent is 20 or 30 or 40
            ? Row($"Kartla avans ({booking.DepositPercent}%)", Money(booking.DepositAmount)) +
              Row("Oteldə qalıq", Money(booking.RemainingAmount))
            : "";
        var note = string.IsNullOrWhiteSpace(booking.SpecialRequests)
            ? ""
            : $"""<div style="margin-top:18px;border:1px solid #C4A484;padding:14px 16px;border-radius:12px"><div style="font-size:11px;letter-spacing:.08em;color:#7A6A5C">XÜSUSİ QEYD</div><div style="margin-top:6px">{H(booking.SpecialRequests)}</div></div>""";

        return $"""
            <html>
            <body style="margin:0;padding:0;background:#F7F1E8;font-family:'Segoe UI',Arial,sans-serif;color:#1C1410">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#F7F1E8;padding:24px 0">
                <tr><td align="center">
                  <table width="640" cellpadding="0" cellspacing="0" style="max-width:640px;background:#F7F1E8">
                    <tr>
                      <td style="background:#1C1410;padding:28px 32px">
                        <table width="100%"><tr>
                          <td>
                            <div style="color:#C4A484;font-size:22px;font-weight:700;letter-spacing:.12em">AE HOTEL</div>
                            <div style="color:#F7F1E8;font-size:13px;margin-top:6px">Rezervasiya təsdiqi</div>
                          </td>
                          <td align="right">
                            <div style="color:#C4A484;font-size:11px;letter-spacing:.08em">BRON KODU</div>
                            <div style="color:#F7F1E8;font-size:18px;font-weight:700;margin-top:4px">{H(booking.Code)}</div>
                          </td>
                        </tr></table>
                      </td>
                    </tr>
                    <tr><td style="height:4px;background:#C4A484;font-size:0;line-height:0">&nbsp;</td></tr>
                    <tr>
                      <td style="padding:28px 32px">
                        <p style="margin:0 0 18px;color:#7A6A5C;font-size:13px">Salam {H(booking.GuestName)}, rezervasiyanız təsdiqləndi. PDF sənəd bu məktuba əlavə olunub.</p>
                        <table width="100%" cellpadding="0" cellspacing="0">
                          <tr>
                            <td width="48%" style="background:#EFE6D8;padding:16px;border-radius:12px;vertical-align:top">
                              <div style="font-size:11px;color:#7A6A5C;letter-spacing:.08em">QONAĞ</div>
                              <div style="font-size:16px;font-weight:700;margin-top:6px">{H(booking.GuestName)}</div>
                              <div style="font-size:13px;color:#7A6A5C;margin-top:4px">{H(booking.GuestEmail)}</div>
                            </td>
                            <td width="4%"></td>
                            <td width="48%" style="background:#EFE6D8;padding:16px;border-radius:12px;vertical-align:top">
                              <div style="font-size:11px;color:#7A6A5C;letter-spacing:.08em">QALMA</div>
                              <div style="font-size:16px;font-weight:700;margin-top:6px">{hotel}</div>
                              <div style="font-size:13px;color:#7A6A5C;margin-top:4px">{H(booking.RoomName)}</div>
                              <div style="font-size:13px;margin-top:4px">{booking.CheckIn} — {booking.CheckOut}</div>
                            </td>
                          </tr>
                        </table>
                        <table width="100%" cellpadding="0" cellspacing="0" style="margin-top:20px">
                          {Row("Otel / xidmət", hotel)}
                          {Row("Qonaq sayı", booking.Guests.ToString())}
                          {Row("Siyahı qiyməti", Money(listTotal))}
                          {discountRow}
                          {Row("Ödəniş", $"{PayMethodAz(booking.PaymentMethod)} · {PayStatusAz(booking.PaymentStatus)}")}
                          {depositRows}
                        </table>
                        <table width="100%" cellpadding="0" cellspacing="0" style="margin-top:8px">
                          <tr>
                            <td style="background:#1C1410;padding:16px 18px;color:#C4A484;font-size:13px">Yekun</td>
                            <td style="background:#1C1410;padding:16px 18px;color:#F7F1E8;font-size:20px;font-weight:700;text-align:right">{Money(booking.TotalPrice)}</td>
                          </tr>
                        </table>
                        {note}
                      </td>
                    </tr>
                    <tr>
                      <td style="background:#1C1410;padding:16px 32px;color:#C4A484;font-size:12px">
                        AE Hotel · info@aehotel.com · (094) 542-4789
                        <div style="margin-top:6px;color:#F7F1E8;font-size:11px">Kart ödənişi demo rejimindədir. Real kart məlumatı saxlanılmır.</div>
                      </td>
                    </tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string Row(string label, string value) =>
        $"""<tr><td style="padding:10px 0;border-bottom:1px solid #EFE6D8;color:#7A6A5C">{label}</td><td style="padding:10px 0;border-bottom:1px solid #EFE6D8;text-align:right;font-weight:600">{value}</td></tr>""";

    private static string Money(decimal value) =>
        $"₼ {value.ToString("0.00", CultureInfo.InvariantCulture)}";

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string PayMethodAz(string method) => method.ToLowerInvariant() switch
    {
        "hotel" => "Oteldə ödəniş (avans kartla)",
        "card" => "Kart",
        _ => string.IsNullOrWhiteSpace(method) ? "—" : H(method)
    };

    private static string PayStatusAz(string status) => status.ToLowerInvariant() switch
    {
        "paid" => "Ödənilib",
        "pending" => "Gözləyir",
        "deposit" => "Avans ödənilib",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : H(status)
    };
}
