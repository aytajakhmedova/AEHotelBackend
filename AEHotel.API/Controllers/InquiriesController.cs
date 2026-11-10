using System.Security.Claims;
using AEHotel.API.Security;
using AEHotel.Service.DTOs.Inquiries;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[Route("api/inquiries")]
public class InquiriesController : ControllerBase
{
    private readonly IInquiryService _inquiries;

    public InquiriesController(IInquiryService inquiries)
    {
        _inquiries = inquiries;
    }

    // null = every question (admins); a hotel manager only gets the questions of the hotel assigned to them.
    private int? Scope() => User.TryGetHotelScope(out var hotelId) ? hotelId : null;

    // Guests may ask without an account; a signed-in guest's question is linked to them.
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<InquiryResponse>> Create(InquiryRequest request, CancellationToken cancellationToken)
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        var created = await _inquiries.CreateAsync(request, userId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<InquiryResponse>>> GetMine(CancellationToken cancellationToken)
        => Ok(await _inquiries.GetMineAsync(User.GetRequiredUserId(), cancellationToken));

    [HttpGet]
    [Authorize(Roles = AppRoles.CatalogEditors)]
    public async Task<ActionResult<IReadOnlyList<InquiryResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _inquiries.GetAllAsync(Scope(), cancellationToken));

    [HttpPut("{id:int}/reply")]
    [Authorize(Roles = AppRoles.CatalogEditors)]
    public async Task<ActionResult<InquiryResponse>> Reply(int id, InquiryReplyRequest request, CancellationToken cancellationToken)
    {
        var staffName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "AE Hotel";
        return Ok(await _inquiries.ReplyAsync(id, request.Reply, staffName, Scope(), cancellationToken));
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = AppRoles.CatalogEditors)]
    public async Task<ActionResult<InquiryResponse>> SetStatus(int id, InquiryStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _inquiries.SetStatusAsync(id, request.Status, Scope(), cancellationToken));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _inquiries.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
