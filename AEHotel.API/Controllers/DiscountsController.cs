using AEHotel.Service.DTOs.Discounts;
using AEHotel.Service.Exceptions;
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

    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<DiscountPackageResponse>>> GetMine(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedException("Authentication is required.");
        return Ok(await _discounts.GetMineAsync(userId, cancellationToken));
    }

    [HttpGet("coupon/{code}")]
    [AllowAnonymous]
    public async Task<ActionResult<DiscountPackageResponse>> GetCoupon(string code, CancellationToken cancellationToken)
    {
        // The signed-in user (if any) is needed for personal coupons such as the welcome coupon.
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var coupon = await _discounts.GetActiveByCodeAsync(code, cancellationToken, userId)
            ?? throw new NotFoundException("Kupon kodu keçərsizdir və ya müddəti bitib.");
        return Ok(coupon);
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
