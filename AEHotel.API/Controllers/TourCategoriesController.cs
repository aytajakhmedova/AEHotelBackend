using AEHotel.Service.DTOs.Tours;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[Route("api/tour-categories")]
public class TourCategoriesController : ControllerBase
{
    private readonly ITourCategoryService _categories;

    public TourCategoriesController(ITourCategoryService categories)
    {
        _categories = categories;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<TourCategoryResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _categories.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<TourCategoryResponse>> Create(TourCategoryRequest request, CancellationToken cancellationToken)
    {
        var created = await _categories.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<TourCategoryResponse>> Update(int id, TourCategoryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _categories.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _categories.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
