using AEHotel.API.Security;
using AEHotel.Service.DTOs.Reviews;
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
}
