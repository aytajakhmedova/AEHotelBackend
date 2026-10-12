using AEHotel.Service.Services.Interfaces;
using AEHotel.Service.DTOs.Flights;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.Exceptions;

namespace AEHotel.Service.Services;

public class FlightService : IFlightService
{
    private readonly IFlightRepository _flights;

    public FlightService(IFlightRepository flights)
    {
        _flights = flights;
    }

    public async Task<IReadOnlyList<FlightResponse>> GetAllAsync(string? origin, string? destination, DateOnly? date, string? cabin, CancellationToken cancellationToken = default)
    {
        var flights = await _flights.GetAllAsync(cancellationToken);
        return flights
            .Where(flight => Matches(flight, origin, destination, date, cabin))
            .Select(Map)
            .ToList();
    }

    public async Task<FlightResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var flight = await _flights.GetByIdAsync(id, tracking: false, cancellationToken)
            ?? throw new NotFoundException($"Flight with id {id} was not found.");
        return Map(flight);
    }

    public async Task<FlightResponse> CreateAsync(FlightRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var flight = new Flight();
        Apply(flight, request);
        await _flights.AddAsync(flight, cancellationToken);
        await _flights.SaveChangesAsync(cancellationToken);
        return Map(flight);
    }

    public async Task<FlightResponse> UpdateAsync(int id, FlightRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var flight = await _flights.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Flight with id {id} was not found.");
        Apply(flight, request);
        await _flights.SaveChangesAsync(cancellationToken);
        return Map(flight);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var flight = await _flights.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Flight with id {id} was not found.");
        _flights.Remove(flight);
        await _flights.SaveChangesAsync(cancellationToken);
    }

    private static bool Matches(Flight flight, string? origin, string? destination, DateOnly? date, string? cabin)
    {
        if (!MatchesPlace(origin, flight.OriginCode, flight.OriginCity))
        {
            return false;
        }

        if (!MatchesPlace(destination, flight.DestinationCode, flight.DestinationCity))
        {
            return false;
        }

        if (date.HasValue && DateOnly.FromDateTime(flight.DepartureAt) != date.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(cabin) &&
            !string.Equals(flight.CabinClass, cabin.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool MatchesPlace(string? query, string code, string city)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var value = query.Trim();
        return value.Contains(code, StringComparison.OrdinalIgnoreCase) ||
               value.Contains(city, StringComparison.OrdinalIgnoreCase) ||
               code.Contains(value, StringComparison.OrdinalIgnoreCase) ||
               city.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    private static void Validate(FlightRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Airline) || string.IsNullOrWhiteSpace(request.FlightNumber))
        {
            throw new ValidationException("Airline and flight number are required.");
        }

        if (string.IsNullOrWhiteSpace(request.OriginCode) || string.IsNullOrWhiteSpace(request.DestinationCode))
        {
            throw new ValidationException("Origin and destination are required.");
        }

        if (request.ArrivalAt <= request.DepartureAt)
        {
            throw new ValidationException("Arrival must be after departure.");
        }

        if (request.Price < 0 || request.SeatsAvailable < 0)
        {
            throw new ValidationException("Price and seats cannot be negative.");
        }
    }

    private static void Apply(Flight flight, FlightRequest request)
    {
        flight.Airline = request.Airline.Trim();
        flight.FlightNumber = request.FlightNumber.Trim();
        flight.OriginCode = request.OriginCode.Trim().ToUpperInvariant();
        flight.OriginCity = request.OriginCity.Trim();
        flight.OriginCountry = request.OriginCountry.Trim();
        flight.DestinationCode = request.DestinationCode.Trim().ToUpperInvariant();
        flight.DestinationCity = request.DestinationCity.Trim();
        flight.DestinationCountry = request.DestinationCountry.Trim();
        flight.DepartureAt = request.DepartureAt;
        flight.ArrivalAt = request.ArrivalAt;
        flight.Duration = request.Duration.Trim();
        flight.Stops = request.Stops;
        flight.Price = request.Price;
        flight.CabinClass = string.IsNullOrWhiteSpace(request.CabinClass) ? "economy" : request.CabinClass.Trim().ToLowerInvariant();
        flight.SeatsAvailable = request.SeatsAvailable;
        flight.Refundable = request.Refundable;
    }

    private static FlightResponse Map(Flight flight)
    {
        return new FlightResponse
        {
            Id = flight.Id,
            Airline = flight.Airline,
            FlightNumber = flight.FlightNumber,
            OriginCode = flight.OriginCode,
            OriginCity = flight.OriginCity,
            OriginCountry = flight.OriginCountry,
            DestinationCode = flight.DestinationCode,
            DestinationCity = flight.DestinationCity,
            DestinationCountry = flight.DestinationCountry,
            DepartureAt = flight.DepartureAt,
            ArrivalAt = flight.ArrivalAt,
            Duration = flight.Duration,
            Stops = flight.Stops,
            Price = flight.Price,
            CabinClass = flight.CabinClass,
            SeatsAvailable = flight.SeatsAvailable,
            Refundable = flight.Refundable
        };
    }
}
