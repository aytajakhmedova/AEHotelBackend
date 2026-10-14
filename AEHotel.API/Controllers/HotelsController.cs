using AEHotel.Service.DTOs.Hotels;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[Route("api/hotels")]
public class HotelsController : ControllerBase
{
    private readonly IHotelService _hotelService;

    public HotelsController(IHotelService hotelService)
    {
        _hotelService = hotelService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HotelResponse>>> GetAll(
        [FromQuery] DateOnly? checkIn,
        [FromQuery] DateOnly? checkOut,
        [FromQuery] int? guests,
        CancellationToken cancellationToken)
    {
        var hotels = await _hotelService.GetAllAsync(checkIn, checkOut, guests, cancellationToken);
        return Ok(hotels);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<HotelResponse>> GetById(
        int id,
        [FromQuery] DateOnly? checkIn,
        [FromQuery] DateOnly? checkOut,
        [FromQuery] int? guests,
        CancellationToken cancellationToken)
    {
        var hotel = await _hotelService.GetByIdAsync(id, checkIn, checkOut, guests, cancellationToken);
        return Ok(hotel);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<HotelResponse>> Create(HotelRequest request, CancellationToken cancellationToken)
    {
        var created = await _hotelService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<HotelResponse>> Update(int id, HotelRequest request, CancellationToken cancellationToken)
    {
        var updated = await _hotelService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _hotelService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
