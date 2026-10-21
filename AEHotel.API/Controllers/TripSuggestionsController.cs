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
        [FromQuery] DateOnly? checkIn,
        [FromQuery] DateOnly? checkOut,
        [FromQuery] int guests = 1,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _suggestions.GetAsync(city, checkIn, checkOut, guests, cancellationToken));
    }
}
