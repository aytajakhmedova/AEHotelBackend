using AEHotel.API.Security;
using AEHotel.Service.DTOs.Bookings;
using AEHotel.Service.Helpers;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[Authorize]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.BookingOps)]
    public async Task<ActionResult<IReadOnlyList<BookingResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var bookings = await _bookingService.GetAllAsync(cancellationToken);
        if (User.TryGetHotelScope(out var hotelId))
        {
            bookings = hotelId == 0
                ? []
                : bookings.Where(item => item.HotelId == hotelId).ToList();
        }
        return Ok(bookings);
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<BookingResponse>>> GetMine(
        [FromQuery] string? bookingType,
        CancellationToken cancellationToken)
    {
        return Ok(await _bookingService.GetMineAsync(User.GetRequiredUserId(), bookingType, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _bookingService.GetByIdAsync(id, User.GetRequiredUserId(), User.IsAdmin(), cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponse>> Create(BookingRequest request, CancellationToken cancellationToken)
    {
        var created = await _bookingService.CreateAsync(request, User.GetRequiredUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BookingResponse>> Update(int id, BookingUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _bookingService.UpdateAsync(id, request, User.GetRequiredUserId(), User.IsAdmin(), cancellationToken));
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = AppRoles.BookingOps)]
    public async Task<ActionResult<BookingResponse>> UpdateStatus(int id, BookingStatusRequest request, CancellationToken cancellationToken)
    {
        var existing = await _bookingService.GetByIdAsync(id, User.GetRequiredUserId(), true, cancellationToken);
        User.EnsureHotelScope(existing.HotelId);
        return Ok(await _bookingService.UpdateStatusAsync(id, request.Status, User.GetRequiredUserId(), true, cancellationToken));
    }

    [HttpGet("{id:int}/receipt")]
    public async Task<IActionResult> Receipt(int id, CancellationToken cancellationToken)
    {
        var booking = await _bookingService.GetByIdAsync(id, User.GetRequiredUserId(), User.IsAdmin(), cancellationToken);
        return Content(BookingReceiptHtml.Build(booking), "text/html; charset=utf-8");
    }

    [HttpPost("{id:int}/email-receipt")]
    public async Task<ActionResult<BookingResponse>> EmailReceipt(int id, CancellationToken cancellationToken)
    {
        return Ok(await _bookingService.SendReceiptEmailAsync(id, User.GetRequiredUserId(), User.IsAdmin(), cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _bookingService.DeleteAsync(id, User.GetRequiredUserId(), User.IsAdmin(), cancellationToken);
        return NoContent();
    }
}
