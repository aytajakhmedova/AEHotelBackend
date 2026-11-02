using AEHotel.Service.DTOs.Bookings;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/trip-suggestions")]
public class TripSuggestionsController : ControllerBase
{
    private readonly ITripSuggestionService _suggestions;

    public TripSuggestionsController(ITripSuggestionService suggestions)
    {
        _suggestions = suggestions;
    }

    [HttpGet]
    public async Task<ActionResult<TripSuggestionResponse>> Get(
        [FromQuery] string? city,
        [FromQuery] string? country,
        [FromQuery] DateOnly? checkIn,
        [FromQuery] DateOnly? checkOut,
        [FromQuery] int guests = 1,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _suggestions.GetAsync(city, country, checkIn, checkOut, guests, cancellationToken));
    }

    [HttpGet("countries")]
    public async Task<ActionResult<IReadOnlyList<TripCountryResponse>>> GetCountries(CancellationToken cancellationToken = default)
    {
        return Ok(await _suggestions.GetCountriesAsync(cancellationToken));
    }

    [HttpGet("by-country")]
    public async Task<ActionResult<TripSuggestionResponse>> GetByCountry(
        [FromQuery] string country,
        [FromQuery] int guests = 1,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _suggestions.GetByCountryAsync(country ?? string.Empty, guests, cancellationToken));
    }

    [HttpGet("by-hotel/{hotelId:int}")]
    public async Task<ActionResult<TripSuggestionResponse>> GetByHotel(
        int hotelId,
        [FromQuery] DateOnly? checkIn,
        [FromQuery] DateOnly? checkOut,
        [FromQuery] int guests = 1,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _suggestions.GetByHotelAsync(hotelId, checkIn, checkOut, guests, cancellationToken));
    }
}
