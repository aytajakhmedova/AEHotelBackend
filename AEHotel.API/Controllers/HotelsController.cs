using AEHotel.API.Security;
using AEHotel.Service.DTOs.Bookings;
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
    private readonly IWebHostEnvironment _environment;

    public HotelsController(IHotelService hotelService, IWebHostEnvironment environment)
    {
        _hotelService = hotelService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HotelResponse>>> GetAll(
        [FromQuery] string? city,
        [FromQuery] DateOnly? checkIn,
        [FromQuery] DateOnly? checkOut,
        [FromQuery] int? guests,
        [FromQuery] int? adults, [FromQuery] int? children, [FromQuery] int? rooms, [FromQuery] string? country,
        [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] string? currency,
        CancellationToken cancellationToken)
    {
        var hotels = await _hotelService.GetAllAsync(city, checkIn, checkOut, guests, cancellationToken, adults, children, rooms, country, minPrice, maxPrice, currency);
        if (User.Identity?.IsAuthenticated == true && User.TryGetHotelScope(out var scopedHotelId))
        {
            hotels = scopedHotelId == 0
                ? []
                : hotels.Where(hotel => hotel.Id == scopedHotelId).ToList();
        }
        return Ok(hotels);
    }

    [HttpGet("debug")]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult> GetAllDebug(CancellationToken cancellationToken)
    {
        // Only available in Development environment for security
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var hotels = await _hotelService.GetAllAsync(null, null, null, null, cancellationToken);
        return Ok(new
        {
            totalCount = hotels.Count,
            hotels = hotels.Select(h => new
            {
                h.Id,
                h.Title,
                h.City,
                roomCount = h.Rooms.Count,
                hasAvailableRooms = h.Rooms.Any(r => r.IsAvailable)
            })
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<HotelResponse>> GetById(
        int id,
        [FromQuery] DateOnly? checkIn,
        [FromQuery] DateOnly? checkOut,
        [FromQuery] int? guests,
        [FromQuery] int? adults, [FromQuery] int? children, [FromQuery] int? rooms,
        CancellationToken cancellationToken)
    {
        var hotel = await _hotelService.GetByIdAsync(id, checkIn, checkOut, guests, cancellationToken, adults, children, rooms);
        return Ok(hotel);
    }

    [HttpPost("{id:int}/quote")]
    public async Task<ActionResult<HotelStayQuote>> Quote(int id, HotelStayRequest request,
        [FromServices] IBookingService bookings, CancellationToken cancellationToken)
        => Ok(await bookings.QuoteHotelAsync(id, request, cancellationToken));

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
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _hotelService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
