using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasinmazYönetimi.Application.Auth;

namespace TasinmazYönetimi.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthSessionsController : ControllerBase
{
    private readonly IAuthSessionService _sessions;
    
    public AuthSessionsController(IAuthSessionService sessions)
    {
        _sessions = sessions;
    }

    // Token'ı sunucuda iptal eder ve çıkış kaydı oluşturur.
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out Guid userId)
            || !long.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Exp), out long expiration))
            return Unauthorized();

        string header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Unauthorized();

        await _sessions.RevokeAsync(userId, header[7..].Trim(),
            DateTimeOffset.FromUnixTimeSeconds(expiration).UtcDateTime, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<CurrentUserResponse> Me()
    {
        string? id = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Ok(new CurrentUserResponse { Id = id });
    }
}
