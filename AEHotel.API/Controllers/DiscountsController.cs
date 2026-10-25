using AEHotel.Service.DTOs.Discounts;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[Route("api/discounts")]
public class DiscountsController : ControllerBase
{
    private readonly IDiscountService _discounts;

    public DiscountsController(IDiscountService discounts)
    {
        _discounts = discounts;
    }

    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<DiscountPackageResponse>>> GetActive(CancellationToken cancellationToken)
    {
        return Ok(await _discounts.GetActiveAsync(cancellationToken));
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<IReadOnlyList<DiscountPackageResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _discounts.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<DiscountPackageResponse>> Create(DiscountPackageRequest request, CancellationToken cancellationToken)
    {
        var created = await _discounts.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<DiscountPackageResponse>> Update(int id, DiscountPackageRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _discounts.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _discounts.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
