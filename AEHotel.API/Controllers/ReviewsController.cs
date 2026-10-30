using AEHotel.API.Security;
using AEHotel.Service.DTOs.Reviews;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[Route("api")]
public class ReviewsController : ControllerBase
{
    private readonly IStayReviewService _reviews;

    public ReviewsController(IStayReviewService reviews)
    {
        _reviews = reviews;
    }

    [HttpGet("hotels/{hotelId:int}/reviews")]
    public async Task<ActionResult<IReadOnlyList<ReviewResponse>>> GetByHotel(int hotelId, CancellationToken cancellationToken)
    {
        return Ok(await _reviews.GetByHotelAsync(hotelId, cancellationToken));
    }

    [HttpGet("reviews/mine")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<ReviewResponse>>> GetMine(CancellationToken cancellationToken)
    {
        return Ok(await _reviews.GetMineAsync(User.GetRequiredUserId(), cancellationToken));
    }

    [HttpGet("reviews")]
    [Authorize(Roles = AppRoles.CatalogEditors)]
    public async Task<ActionResult<IReadOnlyList<ReviewResponse>>> GetAll(CancellationToken cancellationToken)
    {
        int? scope = User.TryGetHotelScope(out var hotelId) ? hotelId : null;
        return Ok(await _reviews.GetAllAsync(scope, cancellationToken));
    }

    [HttpPost("reviews")]
    [Authorize]
    public async Task<ActionResult<ReviewResponse>> Create(ReviewRequest request, CancellationToken cancellationToken)
    {
        var created = await _reviews.CreateAsync(
            request,
            User.GetRequiredUserId(),
            User.Identity?.Name ?? string.Empty,
            cancellationToken);
        return CreatedAtAction(nameof(GetByHotel), new { hotelId = created.HotelId }, created);
    }

    [HttpPut("reviews/{id:int}/approve")]
    [Authorize(Roles = AppRoles.CatalogEditors)]
    public async Task<ActionResult<ReviewResponse>> Approve(int id, CancellationToken cancellationToken)
    {
        int? scope = User.TryGetHotelScope(out var hotelId) ? hotelId : null;
        return Ok(await _reviews.ApproveAsync(id, scope, cancellationToken));
    }

    [HttpDelete("reviews/{id:int}")]
    [Authorize(Roles = AppRoles.CatalogEditors)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        int? scope = User.TryGetHotelScope(out var hotelId) ? hotelId : null;
        await _reviews.DeleteAsync(id, scope, cancellationToken);
        return NoContent();
    }
}
