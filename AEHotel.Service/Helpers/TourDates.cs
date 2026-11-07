using AEHotel.Domain.Entities;

namespace AEHotel.Service.Helpers;

/// <summary>A tour runs on fixed dates; once all of them have passed, guests must no longer see it.</summary>
internal static class TourDates
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>The dates (yyyy-MM-dd) that are today or later.</summary>
    public static List<string> Upcoming(IEnumerable<string> dates)
    {
        var today = Today;
        return dates.Where(date => DateOnly.TryParse(date, out var day) && day >= today).ToList();
    }

    /// <summary>A tour without fixed dates always counts as running.</summary>
    public static bool HasUpcoming(Tour tour)
    {
        var dates = JsonLists.ReadStrings(tour.AvailableDatesJson);
        return dates.Count == 0 || Upcoming(dates).Count > 0;
    }
}
