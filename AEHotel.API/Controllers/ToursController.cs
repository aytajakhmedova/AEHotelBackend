using AEHotel.Service.DTOs.Tours;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[Route("api/tours")]
public class ToursController : ControllerBase
{
    private readonly ITourService _tourService;

    public ToursController(ITourService tourService)
    {
        _tourService = tourService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TourResponse>>> GetAll(
        [FromQuery] string? destination,
        [FromQuery] DateOnly? date,
        [FromQuery] int? guests,
        CancellationToken cancellationToken)
    {
        return Ok(await _tourService.GetAllAsync(destination, date, guests, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TourResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _tourService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<TourResponse>> Create(TourRequest request, CancellationToken cancellationToken)
    {
        var created = await _tourService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<TourResponse>> Update(int id, TourRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _tourService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _tourService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
