using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasinmazYönetimi.Application.Locations;

namespace TasinmazYönetimi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locations;

    public LocationsController(ILocationService locations)
    {
        _locations = locations;
    }

    [HttpGet("cities")]
    public async Task<ActionResult<List<LocationResponse>>> GetCities(
        CancellationToken cancellationToken) =>
        Ok(await _locations.GetCitiesAsync(cancellationToken));

    [HttpGet("cities/{cityId:int:min(1)}/districts")]
    public async Task<ActionResult<List<LocationResponse>>> GetDistricts(
        int cityId, CancellationToken cancellationToken) =>
        Ok(await _locations.GetDistrictsAsync(cityId, cancellationToken));

    [HttpGet("districts/{districtId:int:min(1)}/neighborhoods")]
    public async Task<ActionResult<List<LocationResponse>>> GetNeighborhoods(
        int districtId, CancellationToken cancellationToken) =>
        Ok(await _locations.GetNeighborhoodsAsync(
            districtId, cancellationToken));
}