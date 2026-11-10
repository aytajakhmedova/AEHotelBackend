using System.Security.Claims;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[Route("api/favorites")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _favorites;

    public FavoritesController(IFavoriteService favorites)
    {
        _favorites = favorites;
    }

    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedException("Authentication is required.");

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<int>>> Get(CancellationToken cancellationToken) =>
        Ok(await _favorites.GetHotelIdsAsync(UserId, cancellationToken));

    [HttpPut("{hotelId:int}")]
    public async Task<IActionResult> Add(int hotelId, CancellationToken cancellationToken)
    {
        await _favorites.AddAsync(UserId, hotelId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{hotelId:int}")]
    public async Task<IActionResult> Remove(int hotelId, CancellationToken cancellationToken)
    {
        await _favorites.RemoveAsync(UserId, hotelId, cancellationToken);
        return NoContent();
    }
}
