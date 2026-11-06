using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using AEHotel.Repository.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace AEHotel.Repository.Repositories;

public class LocationRepository : ILocationRepository
{
    private readonly AppDbContext _context;

    public LocationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Country>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Countries
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<City>> GetCitiesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Cities
            .AsNoTracking()
            .Include(item => item.Country)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<City>> GetCitiesByCountryAsync(int countryId, CancellationToken cancellationToken = default)
    {
        return await _context.Cities
            .AsNoTracking()
            .Where(item => item.CountryId == countryId)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TouristPlace>> GetTouristPlacesByCityAsync(int cityId, CancellationToken cancellationToken = default)
    {
        return await _context.TouristPlaces
            .AsNoTracking()
            .Where(item => item.CityId == cityId)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Country?> GetCountryAsync(int id, CancellationToken cancellationToken = default, bool tracking = false)
    {
        var countries = tracking ? _context.Countries.AsTracking() : _context.Countries.AsNoTracking();
        return countries.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<City?> GetCityAsync(int id, CancellationToken cancellationToken = default, bool tracking = false)
    {
        var cities = tracking ? _context.Cities.AsTracking() : _context.Cities.AsNoTracking();
        return cities.Include(item => item.Country)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<TouristPlace?> GetTouristPlaceAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.TouristPlaces.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<IDbContextTransaction> BeginCatalogWriteAsync(CancellationToken cancellationToken)
        => _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

    // Some catalog records predate location IDs. Resolve their stored country/city pair exactly.
    public async Task<(List<Hotel> Hotels, List<Flight> Flights, List<Tour> Tours)> GetUsageAsync(
        Country country, City? city, CancellationToken cancellationToken)
    {
        var name = country.Name;
        var cityName = city?.Name;
        var cityId = city?.Id;
        var hotels = await _context.Hotels.Where(h =>
            (cityId == null ? h.CityRecord!.CountryId == country.Id : h.CityId == cityId) ||
            (h.CityId == null && h.Country.Trim() == name && (cityName == null || h.City.Trim() == cityName)))
            .ToListAsync(cancellationToken);
        var flights = await _context.Flights.Where(f =>
            (f.OriginCountry.Trim() == name && (cityName == null || f.OriginCity.Trim() == cityName)) ||
            (f.DestinationCountry.Trim() == name && (cityName == null || f.DestinationCity.Trim() == cityName)))
            .ToListAsync(cancellationToken);
        var tours = await _context.Tours.Where(t => t.Destination.Contains(",") &&
            t.Destination.Substring(t.Destination.IndexOf(",") + 1).Trim() == name &&
            (cityName == null || t.Destination.Substring(0, (t.Destination + ",").IndexOf(",")).Trim() == cityName))
            .ToListAsync(cancellationToken);
        return (hotels, flights, tours);
    }

    public void Add(Country country) => _context.Countries.Add(country);
    public void Add(City city) => _context.Cities.Add(city);
    public void Remove(Country country) => _context.Countries.Remove(country);
    public void Remove(City city) => _context.Cities.Remove(city);
    public void Remove(Hotel hotel) => _context.Hotels.Remove(hotel);
    public void Remove(Flight flight) => _context.Flights.Remove(flight);
    public void Remove(Tour tour) => _context.Tours.Remove(tour);
    
    public Task DeleteHotelWithRelatedDataAsync(Hotel hotel, CancellationToken cancellationToken)
    {
        // Not used anymore - we use SQL cascade delete
        return Task.CompletedTask;
    }
    
    public async Task DeleteCountryCascadeAsync(int countryId, CancellationToken cancellationToken)
    {
        var countryName = await _context.Countries.Where(c => c.Id == countryId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken);
        if (countryName == null) return;

        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            -- Delete StayReviews first (linked to both Hotel and Booking)
            DELETE FROM StayReviews WHERE HotelId IN (SELECT H.Id FROM Hotels H INNER JOIN Cities C ON H.CityId = C.Id WHERE C.CountryId = {countryId});
            
            -- Delete Payments (linked to Bookings)
            DELETE FROM Payments WHERE BookingId IN (SELECT B.Id FROM Bookings B INNER JOIN Hotels H ON B.HotelId = H.Id INNER JOIN Cities C ON H.CityId = C.Id WHERE C.CountryId = {countryId});
            DELETE FROM Payments WHERE BookingId IN (SELECT B.Id FROM Bookings B INNER JOIN Flights F ON B.FlightId = F.Id WHERE F.OriginCountry = {countryName} OR F.DestinationCountry = {countryName});
            DELETE FROM Payments WHERE BookingId IN (SELECT B.Id FROM Bookings B INNER JOIN Tours T ON B.TourId = T.Id WHERE T.Destination LIKE '%' + {countryName});
            
            -- Delete BookingRooms (linked to Bookings)
            DELETE FROM BookingRooms WHERE BookingId IN (SELECT B.Id FROM Bookings B INNER JOIN Hotels H ON B.HotelId = H.Id INNER JOIN Cities C ON H.CityId = C.Id WHERE C.CountryId = {countryId});
            
            -- Delete Bookings (linked to Hotels, Flights, Tours)
            DELETE FROM Bookings WHERE HotelId IN (SELECT H.Id FROM Hotels H INNER JOIN Cities C ON H.CityId = C.Id WHERE C.CountryId = {countryId});
            DELETE FROM Bookings WHERE FlightId IN (SELECT Id FROM Flights WHERE OriginCountry = {countryName} OR DestinationCountry = {countryName});
            DELETE FROM Bookings WHERE TourId IN (SELECT Id FROM Tours WHERE Destination LIKE '%' + {countryName});
            
            -- Delete Reservations (linked to Hotels)
            DELETE FROM Reservations WHERE HotelId IN (SELECT H.Id FROM Hotels H INNER JOIN Cities C ON H.CityId = C.Id WHERE C.CountryId = {countryId});
            
            -- Delete Rooms (linked to Hotels)
            DELETE FROM Rooms WHERE HotelId IN (SELECT H.Id FROM Hotels H INNER JOIN Cities C ON H.CityId = C.Id WHERE C.CountryId = {countryId});
            
            -- Now delete Hotels
            DELETE FROM Hotels WHERE CityId IN (SELECT Id FROM Cities WHERE CountryId = {countryId});
            
            -- Delete Flights
            DELETE FROM Flights WHERE OriginCountry = {countryName} OR DestinationCountry = {countryName};
            
            -- Delete Tours
            DELETE FROM Tours WHERE Destination LIKE '%' + {countryName};
            
            -- Delete TouristPlaces
            DELETE FROM TouristPlaces WHERE CityId IN (SELECT Id FROM Cities WHERE CountryId = {countryId});
            
            -- Delete Cities
            DELETE FROM Cities WHERE CountryId = {countryId};
            
            -- Delete Country
            DELETE FROM Countries WHERE Id = {countryId};
        ", cancellationToken);
    }
    
    public async Task DeleteCityCascadeAsync(int cityId, CancellationToken cancellationToken)
    {
        var cityName = await _context.Cities.Where(c => c.Id == cityId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken);
        if (cityName == null) return;

        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            -- Delete StayReviews first
            DELETE FROM StayReviews WHERE HotelId IN (SELECT Id FROM Hotels WHERE CityId = {cityId});
            
            -- Delete Payments (for hotel, flight, and tour bookings)
            DELETE FROM Payments WHERE BookingId IN (SELECT Id FROM Bookings WHERE HotelId IN (SELECT Id FROM Hotels WHERE CityId = {cityId}));
            DELETE FROM Payments WHERE BookingId IN (SELECT B.Id FROM Bookings B INNER JOIN Flights F ON B.FlightId = F.Id WHERE F.OriginCity = {cityName} OR F.DestinationCity = {cityName});
            DELETE FROM Payments WHERE BookingId IN (SELECT B.Id FROM Bookings B INNER JOIN Tours T ON B.TourId = T.Id WHERE T.Destination LIKE {cityName} + ',%');
            
            -- Delete BookingRooms
            DELETE FROM BookingRooms WHERE BookingId IN (SELECT Id FROM Bookings WHERE HotelId IN (SELECT Id FROM Hotels WHERE CityId = {cityId}));
            
            -- Delete Bookings (hotel, flight, tour bookings)
            DELETE FROM Bookings WHERE HotelId IN (SELECT Id FROM Hotels WHERE CityId = {cityId});
            DELETE FROM Bookings WHERE FlightId IN (SELECT Id FROM Flights WHERE OriginCity = {cityName} OR DestinationCity = {cityName});
            DELETE FROM Bookings WHERE TourId IN (SELECT Id FROM Tours WHERE Destination LIKE {cityName} + ',%');
            
            -- Delete Reservations
            DELETE FROM Reservations WHERE HotelId IN (SELECT Id FROM Hotels WHERE CityId = {cityId});
            
            -- Delete Rooms
            DELETE FROM Rooms WHERE HotelId IN (SELECT Id FROM Hotels WHERE CityId = {cityId});
            
            -- Delete Hotels
            DELETE FROM Hotels WHERE CityId = {cityId};
            
            -- Delete Flights
            DELETE FROM Flights WHERE OriginCity = {cityName} OR DestinationCity = {cityName};
            
            -- Delete Tours
            DELETE FROM Tours WHERE Destination LIKE {cityName} + ',%';
            
            -- Delete TouristPlaces
            DELETE FROM TouristPlaces WHERE CityId = {cityId};
            
            -- Delete City
            DELETE FROM Cities WHERE Id = {cityId};
        ", cancellationToken);
    }
    
    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
