using System.Globalization;
using AEHotel.Service.DTOs.Bookings;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AEHotel.Service.Helpers;

public static class BookingReceiptPdf
{
    private static readonly Color Ink = Color.FromHex("#1C1410");
    private static readonly Color Gold = Color.FromHex("#C4A484");
    private static readonly Color Cream = Color.FromHex("#F7F1E8");
    private static readonly Color Soft = Color.FromHex("#EFE6D8");
    private static readonly Color Muted = Color.FromHex("#7A6A5C");

    static BookingReceiptPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(BookingResponse booking)
    {
        var listTotal = booking.TotalPrice + booking.DiscountAmount;
        var az = CultureInfo.GetCultureInfo("az-Latn-AZ");
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.PageColor(Cream);
                page.DefaultTextStyle(text => text.FontSize(11).FontFamily("Segoe UI").FontColor(Ink));

                page.Header().Column(header =>
                {
                    header.Item().Background(Ink).PaddingVertical(22).PaddingHorizontal(36).Row(row =>
                    {
                        row.RelativeItem().Column(brand =>
                        {
                            brand.Item().Text("AE HOTEL").FontSize(22).SemiBold().FontColor(Gold);
                            brand.Item().PaddingTop(4).Text("Rezervasiya təsdiqi").FontSize(11).FontColor(Cream);
                        });
                        row.ConstantItem(180).AlignRight().Column(code =>
                        {
                            code.Item().Text("BRON KODU").FontSize(8).FontColor(Gold);
                            code.Item().PaddingTop(4).Text(booking.Code).FontSize(16).SemiBold().FontColor(Cream);
                        });
                    });
                    header.Item().Background(Gold).Height(4);
                });

                page.Content().Padding(36).Column(column =>
                {
                    column.Spacing(16);
                    column.Item().Text("Qonaq məlumatı və qalma detalları aşağıdadır. Bu sənəd e-poçt təsdiqidir.")
                        .FontSize(10).FontColor(Muted);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Background(Soft).Padding(16).Column(guest =>
                        {
                            guest.Spacing(6);
                            guest.Item().Text("QONAĞ").FontSize(8).FontColor(Muted);
                            guest.Item().Text(booking.GuestName).FontSize(14).SemiBold();
                            guest.Item().Text(booking.GuestEmail).FontSize(10).FontColor(Muted);
                            if (!string.IsNullOrWhiteSpace(booking.GuestPhone))
                            {
                                guest.Item().Text(booking.GuestPhone).FontSize(10).FontColor(Muted);
                            }
                        });
                        row.ConstantItem(16);
                        row.RelativeItem().Background(Soft).Padding(16).Column(stay =>
                        {
                            stay.Spacing(6);
                            stay.Item().Text("QALMA").FontSize(8).FontColor(Muted);
                            stay.Item().Text(string.IsNullOrWhiteSpace(booking.HotelName) ? booking.ItemName : booking.HotelName)
                                .FontSize(14).SemiBold();
                            stay.Item().Text(StayLine(booking)).FontSize(10).FontColor(Muted);
                            stay.Item().Text($"Status: {StatusAz(booking.Status)}").FontSize(10);
                        });
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                        });
                        table.Header(head =>
                        {
                            head.Cell().Background(Ink).Padding(10).Text("Detal").FontColor(Cream).SemiBold();
                            head.Cell().Background(Ink).Padding(10).AlignRight().Text("Məbləğ").FontColor(Cream).SemiBold();
                        });
                        AddRow(table, "Otel / xidmət", string.IsNullOrWhiteSpace(booking.HotelName) ? booking.ItemName : booking.HotelName);
                        if (!string.IsNullOrWhiteSpace(booking.RoomName))
                        {
                            AddRow(table, "Otaq", booking.RoomName);
                        }
                        if (booking.CheckIn.HasValue)
                        {
                            AddRow(table, "Tarixlər", $"{booking.CheckIn} — {booking.CheckOut}");
                        }
                        AddRow(table, "Qonaq sayı", booking.Guests.ToString(az));
                        if (booking.Nights > 0)
                        {
                            AddRow(table, "Gecə", booking.Nights.ToString(az));
                        }
                        AddRow(table, "Siyahı qiyməti", Money(listTotal));
                        if (booking.DiscountAmount > 0)
                        {
                            var label = string.IsNullOrWhiteSpace(booking.CouponCode)
                                ? "Endirim"
                                : $"Kupon {booking.CouponCode}";
                            AddRow(table, label, $"− {Money(booking.DiscountAmount)}");
                        }
                        AddRow(table, "Ödəniş üsulu", PayMethodAz(booking.PaymentMethod));
                        AddRow(table, "Ödəniş statusu", PayStatusAz(booking.PaymentStatus));
                        if (booking.DepositPercent is 20 or 30 or 40)
                        {
                            AddRow(table, $"Kartla avans ({booking.DepositPercent}%)", Money(booking.DepositAmount));
                            AddRow(table, "Oteldə qalıq", Money(booking.RemainingAmount));
                        }
                    });

                    column.Item().Background(Ink).Padding(16).Row(row =>
                    {
                        row.RelativeItem().Text("Yekun").FontColor(Gold).FontSize(12);
                        row.ConstantItem(140).AlignRight().Text(Money(booking.TotalPrice)).FontColor(Cream).FontSize(18).SemiBold();
                    });

                    if (!string.IsNullOrWhiteSpace(booking.SpecialRequests))
                    {
                        column.Item().Border(1).BorderColor(Gold).Padding(14).Column(note =>
                        {
                            note.Item().Text("Xüsusi qeyd").FontSize(8).FontColor(Muted);
                            note.Item().PaddingTop(4).Text(booking.SpecialRequests);
                        });
                    }
                });

                page.Footer().Background(Ink).PaddingVertical(14).PaddingHorizontal(36).Row(row =>
                {
                    row.RelativeItem().Column(foot =>
                    {
                        foot.Item().Text("AE Hotel").FontColor(Gold).FontSize(10).SemiBold();
                        foot.Item().Text("info@aehotel.com  ·  (094) 542-4789").FontColor(Cream).FontSize(9);
                    });
                    row.ConstantItem(220).AlignRight().Text("Kart ödənişi demo rejimindədir. Real kart məlumatı saxlanılmır.")
                        .FontColor(Gold).FontSize(8);
                });
            });
        }).GeneratePdf();
    }

    private static void AddRow(TableDescriptor table, string label, string value)
    {
        table.Cell().BorderBottom(1).BorderColor(Soft).PaddingVertical(8).PaddingHorizontal(10).Text(label).FontColor(Muted);
        table.Cell().BorderBottom(1).BorderColor(Soft).PaddingVertical(8).PaddingHorizontal(10).AlignRight().Text(value);
    }

    private static string StayLine(BookingResponse booking)
    {
        if (booking.BookingType == "flight")
        {
            return string.IsNullOrWhiteSpace(booking.Location) ? booking.ItemName : booking.Location;
        }

        if (booking.Nights > 0)
        {
            return $"{booking.Nights} gecə  ·  {booking.Guests} qonaq";
        }

        return $"{booking.Guests} qonaq";
    }

    private static string Money(decimal value) =>
        $"₼ {value.ToString("0.00", CultureInfo.InvariantCulture)}";

    private static string StatusAz(string status) => status.ToLowerInvariant() switch
    {
        "confirmed" => "Təsdiqlənib",
        "checked-in" => "Giriş edilib",
        "checked-out" => "Çıxış edilib",
        "cancelled" => "Ləğv edilib",
        _ => status
    };

    private static string PayMethodAz(string method) => method.ToLowerInvariant() switch
    {
        "hotel" => "Oteldə ödəniş (avans kartla)",
        "card" => "Kart",
        _ => string.IsNullOrWhiteSpace(method) ? "—" : method
    };

    private static string PayStatusAz(string status) => status.ToLowerInvariant() switch
    {
        "paid" => "Ödənilib",
        "pending" => "Gözləyir",
        "deposit" => "Avans ödənilib",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };
}
