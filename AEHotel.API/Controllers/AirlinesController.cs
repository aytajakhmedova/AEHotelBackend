using AEHotel.Service.DTOs.Airlines;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[Route("api/airlines")]
public class AirlinesController : ControllerBase
{
    private readonly IAirlineService _airlines;

    public AirlinesController(IAirlineService airlines)
    {
        _airlines = airlines;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<AirlineResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _airlines.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<AirlineResponse>> Create(AirlineRequest request, CancellationToken cancellationToken)
    {
        var created = await _airlines.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<AirlineResponse>> Update(int id, AirlineRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _airlines.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _airlines.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
