using AEHotel.Service.DTOs.Flights;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[Route("api/flights")]
public class FlightsController : ControllerBase
{
    private readonly IFlightService _flightService;

    public FlightsController(IFlightService flightService)
    {
        _flightService = flightService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FlightResponse>>> GetAll(
        [FromQuery] string? origin,
        [FromQuery] string? destination,
        [FromQuery] DateOnly? date,
        [FromQuery] int? passengers,
        [FromQuery] string? cabin,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        var flights = await _flightService.GetAllAsync(origin, destination, date, passengers, cabin, sort, cancellationToken);
        return Ok(flights);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FlightResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _flightService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<FlightResponse>> Create(FlightRequest request, CancellationToken cancellationToken)
    {
        var created = await _flightService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<FlightResponse>> Update(int id, FlightRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _flightService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _flightService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
