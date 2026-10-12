using AEHotel.Service.DTOs.Reservations;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReservationResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var reservations = await _reservationService.GetAllAsync(cancellationToken);
        return Ok(reservations);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservationResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var reservation = await _reservationService.GetByIdAsync(id, cancellationToken);
        return Ok(reservation);
    }

    [HttpPost]
    public async Task<ActionResult<ReservationResponse>> Create(ReservationRequest request, CancellationToken cancellationToken)
    {
        var created = await _reservationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReservationResponse>> Update(int id, ReservationRequest request, CancellationToken cancellationToken)
    {
        var updated = await _reservationService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _reservationService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
