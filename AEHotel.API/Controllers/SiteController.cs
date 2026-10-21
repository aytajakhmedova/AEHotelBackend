using AEHotel.Service.DTOs.Site;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[Route("api/site")]
public class SiteController : ControllerBase
{
    private readonly ISiteContentService _site;

    public SiteController(ISiteContentService site)
    {
        _site = site;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<SiteContentResponse>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _site.GetPublicAsync(cancellationToken));
    }

    [HttpGet("cities")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<SiteCityResponse>>> GetCities(CancellationToken cancellationToken)
    {
        return Ok(await _site.GetCitiesAsync(cancellationToken));
    }

    [HttpPut("settings")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<SiteSettingsResponse>> UpdateSettings(SiteSettingsRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _site.UpdateSettingsAsync(request, cancellationToken));
    }

    [HttpGet("menu-items")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<IReadOnlyList<SiteMenuItemResponse>>> GetMenuItems(CancellationToken cancellationToken)
    {
        return Ok(await _site.GetMenuItemsAsync(cancellationToken));
    }

    [HttpPost("menu-items")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<SiteMenuItemResponse>> CreateMenuItem(SiteMenuItemRequest request, CancellationToken cancellationToken)
    {
        var created = await _site.CreateMenuItemAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetMenuItems), new { id = created.Id }, created);
    }

    [HttpPut("menu-items/{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<SiteMenuItemResponse>> UpdateMenuItem(int id, SiteMenuItemRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _site.UpdateMenuItemAsync(id, request, cancellationToken));
    }

    [HttpDelete("menu-items/{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteMenuItem(int id, CancellationToken cancellationToken)
    {
        await _site.DeleteMenuItemAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("slides")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<IReadOnlyList<SiteSlideResponse>>> GetSlides(CancellationToken cancellationToken)
    {
        return Ok(await _site.GetSlidesAsync(cancellationToken));
    }

    [HttpPost("slides")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<SiteSlideResponse>> CreateSlide(SiteSlideRequest request, CancellationToken cancellationToken)
    {
        var created = await _site.CreateSlideAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetSlides), new { id = created.Id }, created);
    }

    [HttpPut("slides/{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<SiteSlideResponse>> UpdateSlide(int id, SiteSlideRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _site.UpdateSlideAsync(id, request, cancellationToken));
    }

    [HttpDelete("slides/{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteSlide(int id, CancellationToken cancellationToken)
    {
        await _site.DeleteSlideAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("advantages")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<IReadOnlyList<SiteAdvantageResponse>>> GetAdvantages(CancellationToken cancellationToken)
    {
        return Ok(await _site.GetAdvantagesAsync(cancellationToken));
    }

    [HttpPost("advantages")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<SiteAdvantageResponse>> CreateAdvantage(SiteAdvantageRequest request, CancellationToken cancellationToken)
    {
        var created = await _site.CreateAdvantageAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAdvantages), new { id = created.Id }, created);
    }

    [HttpPut("advantages/{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<SiteAdvantageResponse>> UpdateAdvantage(int id, SiteAdvantageRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _site.UpdateAdvantageAsync(id, request, cancellationToken));
    }

    [HttpDelete("advantages/{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteAdvantage(int id, CancellationToken cancellationToken)
    {
        await _site.DeleteAdvantageAsync(id, cancellationToken);
        return NoContent();
    }
}
