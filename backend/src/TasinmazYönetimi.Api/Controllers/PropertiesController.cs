using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasinmazYönetimi.Application.Properties;

namespace TasinmazYönetimi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/properties")]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _properties;

    public PropertiesController(IPropertyService properties)
    {
        _properties = properties;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId))
            return Unauthorized();

        try
        {
            PropertyResponse result = await _properties.CreateAsync(
                userId, request, cancellationToken);

            return CreatedAtAction(
                nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId))
            return Unauthorized();

        var result = await _properties.ListAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId))
            return Unauthorized();

        PropertyResponse? result = await _properties.GetByIdAsync(
            userId, id, cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(
            User.FindFirstValue(JwtRegisteredClaimNames.Sub),
            out userId);
}