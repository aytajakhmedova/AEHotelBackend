using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using AEHotel.Service.DTOs.Airlines;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Service.Services;

public class AirlineService : IAirlineService
{
    private readonly AppDbContext _db;

    public AirlineService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AirlineResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var airlines = await _db.Airlines.AsNoTracking().OrderBy(item => item.Name).ToListAsync(cancellationToken);
        var counts = await _db.Flights.AsNoTracking()
            .GroupBy(flight => flight.Airline)
            .Select(group => new { Name = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        return airlines
            .Select(item => Map(item, counts.FirstOrDefault(c => string.Equals(c.Name, item.Name, StringComparison.OrdinalIgnoreCase))?.Count ?? 0))
            .ToList();
    }

    public async Task<string?> FindNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var value = (name ?? string.Empty).Trim();
        return await _db.Airlines.AsNoTracking()
            .Where(item => item.Name == value)
            .Select(item => item.Name)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AirlineResponse> CreateAsync(AirlineRequest request, CancellationToken cancellationToken = default)
    {
        Normalize(request);
        Validate(request);
        if (await _db.Airlines.AnyAsync(item => item.Name == request.Name, cancellationToken))
        {
            throw new ValidationException("An airline with this name already exists.");
        }

        var airline = new Airline { Name = request.Name, Code = request.Code };
        _db.Airlines.Add(airline);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(airline, 0);
    }

    public async Task<AirlineResponse> UpdateAsync(int id, AirlineRequest request, CancellationToken cancellationToken = default)
    {
        Normalize(request);
        Validate(request);
        var airline = await _db.Airlines.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Airline with id {id} was not found.");
        if (await _db.Airlines.AnyAsync(item => item.Name == request.Name && item.Id != id, cancellationToken))
        {
            throw new ValidationException("An airline with this name already exists.");
        }

        // Flights keep the airline by name, so a rename has to follow them.
        var oldName = airline.Name;
        var flights = await _db.Flights.Where(flight => flight.Airline == oldName).ToListAsync(cancellationToken);
        foreach (var flight in flights)
        {
            flight.Airline = request.Name;
        }

        airline.Name = request.Name;
        airline.Code = request.Code;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(airline, flights.Count);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var airline = await _db.Airlines.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Airline with id {id} was not found.");
        if (await _db.Flights.AnyAsync(flight => flight.Airline == airline.Name, cancellationToken))
        {
            throw new ValidationException("This airline cannot be deleted because it has flights.");
        }

        _db.Airlines.Remove(airline);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void Normalize(AirlineRequest request)
    {
        request.Name = (request.Name ?? string.Empty).Trim();
        request.Code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static void Validate(AirlineRequest request)
    {
        if (request.Name.Length is 0 or > 100)
        {
            throw new ValidationException("Airline name is required (up to 100 characters).");
        }

        if (request.Code.Length > 3)
        {
            throw new ValidationException("Airline code can be at most 3 characters.");
        }
    }

    private static AirlineResponse Map(Airline airline, int flightCount) => new()
    {
        Id = airline.Id,
        Name = airline.Name,
        Code = airline.Code,
        FlightCount = flightCount,
    };
}
