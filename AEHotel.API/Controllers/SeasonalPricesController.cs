using AEHotel.Service.DTOs.SeasonalPrices;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[Route("api/seasonal-prices")]
public class SeasonalPricesController : ControllerBase
{
    private readonly ISeasonalPriceService _prices;

    public SeasonalPricesController(ISeasonalPriceService prices)
    {
        _prices = prices;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<SeasonalPriceResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _prices.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<SeasonalPriceResponse>> Create(SeasonalPriceRequest request, CancellationToken cancellationToken)
    {
        var created = await _prices.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<SeasonalPriceResponse>> Update(int id, SeasonalPriceRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _prices.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _prices.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
