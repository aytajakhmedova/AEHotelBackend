using AEHotel.Service.Exceptions;

namespace AEHotel.Service.Helpers;

internal readonly record struct StayParty(int Adults, int Children, int Rooms)
{
    public int Guests => Adults + Children;

    public static StayParty From(int? adults, int? children, int? rooms, int? legacyGuests)
    {
        int kids = children ?? 0;
        long grown = adults ?? ((long)(legacyGuests ?? 1) - kids);
        long total = grown + kids;
        int count = rooms ?? 1;
        if (grown < 1 || kids < 0 || total > int.MaxValue || count < 1 || count > total)
            throw new ValidationException("Select at least one adult and at least one guest per room.");
        return new((int)grown, kids, count);
    }

    // Give every room one guest, then distribute the rest across free beds.
    // Filling the first two places in each room first preserves the existing extra-guest tariff.
    public (int Adults, int Children)[] Allocate(IReadOnlyList<int> capacities)
    {
        if (capacities.Count != Rooms || capacities.Any(c => c < 1) || capacities.Sum(c => (long)c) < Guests)
            throw new ValidationException("The selected rooms cannot accommodate this party.");
        var counts = Enumerable.Repeat(1, Rooms).ToArray();
        int left = Guests - Rooms;
        foreach (var limit in new[] { 2, int.MaxValue })
        {
            for (int i = 0; i < Rooms && left > 0; i++)
            {
                int add = Math.Min(left, Math.Min(limit, capacities[i]) - counts[i]);
                counts[i] += add;
                left -= add;
            }
        }
        int remainingAdults = Adults;
        var adults = new int[Rooms];
        for (int i = 0; i < Rooms && remainingAdults > 0; i++, remainingAdults--)
            adults[i] = 1;
        for (int i = 0; i < Rooms && remainingAdults > 0; i++)
        {
            int add = Math.Min(remainingAdults, counts[i] - adults[i]);
            adults[i] += add;
            remainingAdults -= add;
        }
        return counts.Select((n, i) => (adults[i], n - adults[i])).ToArray();
    }
}
