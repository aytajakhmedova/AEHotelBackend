using AEHotel.Service.DTOs.Bookings;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookingResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _bookingService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _bookingService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponse>> Create(BookingRequest request, CancellationToken cancellationToken)
    {
        var created = await _bookingService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BookingResponse>> Update(int id, BookingUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _bookingService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<BookingResponse>> UpdateStatus(int id, BookingStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _bookingService.UpdateStatusAsync(id, request.Status, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _bookingService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
