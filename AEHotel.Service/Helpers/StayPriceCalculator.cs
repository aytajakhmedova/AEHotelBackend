namespace AEHotel.Service.Helpers;

public static class StayPriceCalculator
{
    public const decimal TaxRate = 0.18m;

    public const decimal ExtraGuestFeePerNight = 50m;

    public static StayPrice Calculate(decimal pricePerNight, DateOnly checkIn, DateOnly checkOut, int guests)
    {
        var nights = checkOut.DayNumber - checkIn.DayNumber;
        if (nights <= 0)
        {
            return new StayPrice(0, 0, 0, 0, 0, 0);
        }

        var basePrice = pricePerNight * nights;
        var extraGuests = Math.Max(0, guests - 2);
        var extraGuestFee = extraGuests * ExtraGuestFeePerNight * nights;
        var subtotal = basePrice + extraGuestFee;
        var tax = Math.Round(subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
        var total = Math.Round(subtotal + tax, 2, MidpointRounding.AwayFromZero);
        return new StayPrice(nights, basePrice, extraGuestFee, subtotal, tax, total);
    }
}

public readonly record struct StayPrice(
    int Nights,
    decimal BasePrice,
    decimal ExtraGuestFee,
    decimal Subtotal,
    decimal Tax,
    decimal Total);
