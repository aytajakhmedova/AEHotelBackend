using AEHotel.API.Security;
using AEHotel.Service.DTOs.Hotels;
using AEHotel.Service.Exceptions;
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
        [FromQuery] string? city,
        [FromQuery] DateOnly? checkIn,
        [FromQuery] DateOnly? checkOut,
        [FromQuery] int? guests,
        CancellationToken cancellationToken)
    {
        var hotels = await _hotelService.GetAllAsync(city, checkIn, checkOut, guests, cancellationToken);
        if (User.Identity?.IsAuthenticated == true && User.TryGetHotelScope(out var scopedHotelId))
        {
            hotels = scopedHotelId == 0
                ? []
                : hotels.Where(hotel => hotel.Id == scopedHotelId).ToList();
        }
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
    [Authorize(Roles = AppRoles.CatalogEditors)]
    public async Task<ActionResult<HotelResponse>> Create(HotelRequest request, CancellationToken cancellationToken)
    {
        if (User.TryGetHotelScope(out _))
        {
            throw new ForbiddenException("Yalnız admin yeni otel əlavə edə bilər.");
        }
        var created = await _hotelService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.CatalogEditors)]
    public async Task<ActionResult<HotelResponse>> Update(int id, HotelRequest request, CancellationToken cancellationToken)
    {
        User.EnsureHotelScope(id);
        var updated = await _hotelService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.CatalogEditors)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        User.EnsureHotelScope(id);
        await _hotelService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
