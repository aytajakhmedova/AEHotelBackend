using AEHotel.Domain.Entities;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.DTOs.Site;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Services.Interfaces;

namespace AEHotel.Service.Services;

public class LocationService : ILocationService
{
    private readonly ILocationRepository _locations;

    public LocationService(ILocationRepository locations)
    {
        _locations = locations;
    }

    public async Task<IReadOnlyList<CountryOptionResponse>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        return (await _locations.GetCountriesAsync(cancellationToken))
            .Select(CountryOption)
            .ToList();
    }

    public async Task<IReadOnlyList<CityOptionResponse>> GetCitiesAsync(int? countryId, CancellationToken cancellationToken = default)
    {
        if (countryId is int id && await _locations.GetCountryAsync(id, cancellationToken) == null)
        {
            throw new NotFoundException($"Country with id {countryId} was not found.");
        }

        var cities = countryId.HasValue ? await _locations.GetCitiesByCountryAsync(countryId.Value, cancellationToken)
            : await _locations.GetCitiesAsync(cancellationToken);
        return cities
            .Select(CityOption)
            .ToList();
    }

    public async Task<IReadOnlyList<TouristPlaceOptionResponse>> GetTouristPlacesAsync(int cityId, CancellationToken cancellationToken = default)
    {
        if (await _locations.GetCityAsync(cityId, cancellationToken) == null)
        {
            throw new NotFoundException($"City with id {cityId} was not found.");
        }

        return (await _locations.GetTouristPlacesByCityAsync(cityId, cancellationToken))
            .Select(place => new TouristPlaceOptionResponse { Id = place.Id, Name = place.Name, CityId = place.CityId })
            .ToList();
    }

    public async Task<CountryOptionResponse> SaveCountryAsync(int? id, CountryRequest request, CancellationToken ct)
    {
        var name = LocationName(request.Name);
        var iso = (request.IsoCode ?? "").Trim().ToUpperInvariant();
        if (iso.Length != 0 && (iso.Length != 2 || !iso.All(char.IsAsciiLetter)))
            throw new ValidationException("ISO kodu iki latın hərfindən ibarət olmalıdır.");

        await using var tx = await _locations.BeginCatalogWriteAsync(ct);
        var country = id.HasValue ? await FindCountry(id.Value, ct) : new Country();
        var countries = await _locations.GetCountriesAsync(ct);
        if (countries.Any(c => c.Id != id && Same(c.Name, name)))
            throw new ValidationException("Bu adda ölkə artıq mövcuddur.");
        if (!id.HasValue)
            _locations.Add(country);
        else if (country.Name != name)
            await RenameReferencesAsync(country, null, name, null, ct);

        country.Name = name;
        country.IsoCode = iso;
        await _locations.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return CountryOption(country);
    }

    public async Task<CityOptionResponse> SaveCityAsync(int? id, CityRequest request, CancellationToken ct)
    {
        var name = LocationName(request.Name);
        await using var tx = await _locations.BeginCatalogWriteAsync(ct);
        var country = await _locations.GetCountryAsync(request.CountryId, ct)
            ?? throw new ValidationException("Siyahıdan mövcud ölkə seçin.");
        var city = id.HasValue ? await FindCity(id.Value, ct) : new City();
        var cities = await _locations.GetCitiesByCountryAsync(country.Id, ct);
        if (cities.Any(c => c.Id != id && Same(c.Name, name)))
            throw new ValidationException("Bu ölkədə eyni adlı şəhər artıq mövcuddur.");
        if (!id.HasValue)
            _locations.Add(city);
        else if (city.Name != name || city.CountryId != country.Id)
            await RenameReferencesAsync(city.Country!, city, country.Name, name, ct);

        city.Name = name;
        city.CountryId = country.Id;
        await _locations.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return CityOption(city);
    }

    public async Task DeleteCountryAsync(int id, CancellationToken ct)
    {
        await using var tx = await _locations.BeginCatalogWriteAsync(ct);
        var country = await FindCountry(id, ct);
        
        // Use raw SQL to cascade delete everything related to this country
        // This bypasses EF Core's FK constraints and deletes in database directly
        await _locations.DeleteCountryCascadeAsync(country.Id, ct);
        
        await tx.CommitAsync(ct);
    }

    public async Task DeleteCityAsync(int id, CancellationToken ct)
    {
        await using var tx = await _locations.BeginCatalogWriteAsync(ct);
        var city = await FindCity(id, ct);
        
        // Use raw SQL to cascade delete everything related to this city
        await _locations.DeleteCityCascadeAsync(city.Id, ct);
        
        await tx.CommitAsync(ct);
    }

    private async Task EnsureUnusedAsync(Country country, City? city, CancellationToken ct)
    {
        var (hotels, flights, tours) = await _locations.GetUsageAsync(country, city, ct);
        if (hotels.Count + flights.Count + tours.Count > 0)
            throw new ValidationException("Bu məkan otel, uçuş və ya turda istifadə olunur və silinə bilməz.");
    }

    private async Task RenameReferencesAsync(Country oldCountry, City? oldCity, string countryName, string? cityName, CancellationToken ct)
    {
        var (hotels, flights, tours) = await _locations.GetUsageAsync(oldCountry, oldCity, ct);
        foreach (var hotel in hotels)
        {
            hotel.Country = countryName;
            if (cityName != null) hotel.City = cityName;
        }
        foreach (var flight in flights)
        {
            if (Same(flight.OriginCountry, oldCountry.Name) && (oldCity == null || Same(flight.OriginCity, oldCity.Name)))
            {
                flight.OriginCountry = countryName;
                if (cityName != null) flight.OriginCity = cityName;
            }
            if (Same(flight.DestinationCountry, oldCountry.Name) && (oldCity == null || Same(flight.DestinationCity, oldCity.Name)))
            {
                flight.DestinationCountry = countryName;
                if (cityName != null) flight.DestinationCity = cityName;
            }
        }
        foreach (var tour in tours)
        {
            var city = cityName ?? tour.Destination.Split(',', 2)[0].Trim();
            var destination = $"{city}, {countryName}";
            if (city.Contains(',') || destination.Length > 200)
                throw new ValidationException("Ad əlaqəli turun məkan sahəsinə uyğun gəlmir.");
            tour.Destination = destination;
        }
    }

    private async Task<Country> FindCountry(int id, CancellationToken ct) =>
        await _locations.GetCountryAsync(id, ct, tracking: true) ?? throw new NotFoundException("Ölkə tapılmadı.");

    private async Task<City> FindCity(int id, CancellationToken ct) =>
        await _locations.GetCityAsync(id, ct, tracking: true) ?? throw new NotFoundException("Şəhər tapılmadı.");

    private static string LocationName(string? value)
    {
        var name = value?.Trim() ?? "";
        if (name.Length is 0 or > 100) throw new ValidationException("Ad 1–100 simvoldan ibarət olmalıdır.");
        return name;
    }

    private static bool Same(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    private static CountryOptionResponse CountryOption(Country country) =>
        new() { Id = country.Id, Name = country.Name, IsoCode = country.IsoCode };
    private static CityOptionResponse CityOption(City city) =>
        new() { Id = city.Id, Name = city.Name, CountryId = city.CountryId };
}
