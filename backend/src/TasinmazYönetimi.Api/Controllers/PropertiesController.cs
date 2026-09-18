using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasinmazYönetimi.Application.Properties;
using TasinmazYönetimi.Domain.Enums;

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
    [Authorize(Roles = nameof(UserRole.User))]
    public async Task<IActionResult> Create(
        [FromBody] CreatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId))
            return Unauthorized();

        try
        {
            PropertyResponse result =
                await _properties.CreateAsync(
                    userId,
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId))
            return Unauthorized();

        if (!TryGetUserRole(out UserRole userRole))
            return Forbid();

        IReadOnlyList<PropertyResponse> result =
            await _properties.ListAsync(
                userId,
                userRole,
                cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId))
            return Unauthorized();

        if (!TryGetUserRole(out UserRole userRole))
            return Forbid();

        PropertyResponse? result =
            await _properties.GetByIdAsync(
                userId,
                userRole,
                id,
                cancellationToken);

        return result is null
            ? NotFound()
            : Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.User))]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId))
            return Unauthorized();

        try
        {
            PropertyResponse? result =
                await _properties.UpdateAsync(
                    userId,
                    id,
                    request,
                    cancellationToken);

            return result is null
                ? NotFound()
                : Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.User))]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId))
            return Unauthorized();

        bool deleted = await _properties.DeleteAsync(
            userId,
            id,
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound();
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(
            User.FindFirstValue(JwtRegisteredClaimNames.Sub),
            out userId);

    private bool TryGetUserRole(out UserRole userRole) =>
        Enum.TryParse(
            User.FindFirstValue(ClaimTypes.Role),
            ignoreCase: true,
            out userRole);
}