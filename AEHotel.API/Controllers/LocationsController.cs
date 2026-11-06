using AEHotel.Service.DTOs.Site;
using AEHotel.Service.Services.Interfaces;
using AEHotel.Service.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

/// <summary>Country > city > tourist place lists used by the cascading dropdowns.</summary>
[ApiController]
[Authorize(Roles = AppRoles.Staff)]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locations;

    public LocationsController(ILocationService locations)
    {
        _locations = locations;
    }

    [AllowAnonymous]
    [HttpGet("countries")]
    public async Task<ActionResult<IReadOnlyList<CountryOptionResponse>>> GetCountries(CancellationToken cancellationToken)
    {
        return Ok(await _locations.GetCountriesAsync(cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("countries/{countryId:int}/cities")]
    public async Task<ActionResult<IReadOnlyList<CityOptionResponse>>> GetCities(int countryId, CancellationToken cancellationToken)
    {
        return Ok(await _locations.GetCitiesAsync(countryId, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("cities/{cityId:int}/tourist-places")]
    public async Task<ActionResult<IReadOnlyList<TouristPlaceOptionResponse>>> GetTouristPlaces(int cityId, CancellationToken cancellationToken)
    {
        return Ok(await _locations.GetTouristPlacesAsync(cityId, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("cities")]
    public async Task<ActionResult<IReadOnlyList<CityOptionResponse>>> GetAllCities(CancellationToken ct)
        => Ok(await _locations.GetCitiesAsync(null, ct));

    [HttpPost("countries")]
    public async Task<ActionResult<CountryOptionResponse>> CreateCountry(CountryRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await _locations.SaveCountryAsync(null, request, ct));

    [HttpPut("countries/{id:int}")]
    public async Task<ActionResult<CountryOptionResponse>> UpdateCountry(int id, CountryRequest request, CancellationToken ct)
        => Ok(await _locations.SaveCountryAsync(id, request, ct));

    [HttpDelete("countries/{id:int}")]
    public async Task<IActionResult> DeleteCountry(int id, CancellationToken ct)
    {
        await _locations.DeleteCountryAsync(id, ct);
        return NoContent();
    }

    [HttpPost("cities")]
    public async Task<ActionResult<CityOptionResponse>> CreateCity(CityRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await _locations.SaveCityAsync(null, request, ct));

    [HttpPut("cities/{id:int}")]
    public async Task<ActionResult<CityOptionResponse>> UpdateCity(int id, CityRequest request, CancellationToken ct)
        => Ok(await _locations.SaveCityAsync(id, request, ct));

    [HttpDelete("cities/{id:int}")]
    public async Task<IActionResult> DeleteCity(int id, CancellationToken ct)
    {
        await _locations.DeleteCityAsync(id, ct);
        return NoContent();
    }
}
