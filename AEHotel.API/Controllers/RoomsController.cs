using AEHotel.Service.DTOs.Rooms;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var rooms = await _roomService.GetAllAsync(cancellationToken);
        return Ok(rooms);
    }

    [HttpGet("/api/hotels/{hotelId:int}/rooms")]
    public async Task<ActionResult<IReadOnlyList<RoomResponse>>> GetByHotel(
        int hotelId,
        [FromQuery] DateOnly? checkIn,
        [FromQuery] DateOnly? checkOut,
        [FromQuery] int? guests,
        CancellationToken cancellationToken)
    {
        var rooms = await _roomService.GetByHotelIdAsync(hotelId, checkIn, checkOut, guests, cancellationToken);
        return Ok(rooms);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var room = await _roomService.GetByIdAsync(id, cancellationToken);
        return Ok(room);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<RoomResponse>> Create(RoomRequest request, CancellationToken cancellationToken)
    {
        var created = await _roomService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<RoomResponse>> Update(int id, RoomRequest request, CancellationToken cancellationToken)
    {
        var updated = await _roomService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpGet("{id:int}/quote")]
    public async Task<ActionResult<RoomQuoteResponse>> Quote(
        int id,
        [FromQuery] DateOnly checkIn,
        [FromQuery] DateOnly checkOut,
        [FromQuery] int guests = 1,
        [FromQuery] int? excludeBookingId = null,
        CancellationToken cancellationToken = default)
    {
        var quote = await _roomService.QuoteAsync(id, checkIn, checkOut, guests, excludeBookingId, cancellationToken);
        return Ok(quote);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _roomService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
