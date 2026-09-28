using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasinmazYönetimi.Application.Users;
using TasinmazYönetimi.Domain.Enums;


namespace TasinmazYönetimi.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class UsersController : ControllerBase
{
    private readonly IUserManagementService _users;

    public UsersController(IUserManagementService users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] UserQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _users.ListAsync(
            query,
            cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        UserResponse? user = await _users.GetByIdAsync(
            id,
            cancellationToken);

        return user is null
            ? NotFound(new { message = "User not found." })
            : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid adminUserId))
            return Unauthorized();

        try
        {
            UserResponse user = await _users.CreateAsync(
                adminUserId,
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = user.Id },
                user);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid adminUserId))
            return Unauthorized();

        try
        {
            UserResponse? user = await _users.UpdateAsync(
                adminUserId,
                id,
                request,
                cancellationToken);

            return user is null
                ? NotFound(new { message = "User not found." })
                : Ok(user);
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
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid adminUserId))
            return Unauthorized();

        if (id == adminUserId)
        {
            return BadRequest(new
            {
                message = "You cannot delete your own account."
            });
        }

        bool deleted = await _users.DeleteAsync(
            adminUserId,
            id,
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound(new { message = "User not found." });
    }

    private bool TryGetUserId(out Guid userId)
    {
        string? value =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("userId");

        return Guid.TryParse(value, out userId);
    }
}