using AEHotel.App.Hotels;
using AEHotel.Service.Hotels;
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
    public async Task<ActionResult<IReadOnlyList<HotelResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var hotels = await _hotelService.GetAllAsync(cancellationToken);
        return Ok(hotels);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<HotelResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var hotel = await _hotelService.GetByIdAsync(id, cancellationToken);
        return Ok(hotel);
    }

    [HttpPost]
    public async Task<ActionResult<HotelResponse>> Create(HotelRequest request, CancellationToken cancellationToken)
    {
        var created = await _hotelService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<HotelResponse>> Update(int id, HotelRequest request, CancellationToken cancellationToken)
    {
        var updated = await _hotelService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _hotelService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
