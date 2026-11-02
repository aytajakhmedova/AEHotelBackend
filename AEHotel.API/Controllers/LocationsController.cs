using AEHotel.Service.DTOs.Site;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

/// <summary>Country > city > tourist place lists used by the cascading dropdowns.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locations;

    public LocationsController(ILocationService locations)
    {
        _locations = locations;
    }

    [HttpGet("countries")]
    public async Task<ActionResult<IReadOnlyList<CountryOptionResponse>>> GetCountries(CancellationToken cancellationToken)
    {
        return Ok(await _locations.GetCountriesAsync(cancellationToken));
    }

    [HttpGet("countries/{countryId:int}/cities")]
    public async Task<ActionResult<IReadOnlyList<CityOptionResponse>>> GetCities(int countryId, CancellationToken cancellationToken)
    {
        return Ok(await _locations.GetCitiesAsync(countryId, cancellationToken));
    }

    [HttpGet("cities/{cityId:int}/tourist-places")]
    public async Task<ActionResult<IReadOnlyList<TouristPlaceOptionResponse>>> GetTouristPlaces(int cityId, CancellationToken cancellationToken)
    {
        return Ok(await _locations.GetTouristPlacesAsync(cityId, cancellationToken));
    }
}
