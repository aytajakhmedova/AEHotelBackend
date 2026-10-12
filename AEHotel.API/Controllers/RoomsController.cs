using AEHotel.App.Rooms;
using AEHotel.Service.Rooms;
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
    public async Task<ActionResult<IReadOnlyList<RoomResponse>>> GetByHotel(int hotelId, CancellationToken cancellationToken)
    {
        var rooms = await _roomService.GetByHotelIdAsync(hotelId, cancellationToken);
        return Ok(rooms);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var room = await _roomService.GetByIdAsync(id, cancellationToken);
        return Ok(room);
    }

    [HttpPost]
    public async Task<ActionResult<RoomResponse>> Create(RoomRequest request, CancellationToken cancellationToken)
    {
        var created = await _roomService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoomResponse>> Update(int id, RoomRequest request, CancellationToken cancellationToken)
    {
        var updated = await _roomService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _roomService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
