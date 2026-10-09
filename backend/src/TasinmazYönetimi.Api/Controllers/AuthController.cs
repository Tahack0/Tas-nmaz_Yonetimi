using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasinmazYönetimi.Api.Security;
using TasinmazYönetimi.Application.Auth;

namespace TasinmazYönetimi.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IRegistrationService _registration;
    private readonly ILoginService _login;
    private readonly JwtTokenService _tokens;
    private readonly IAuthSessionService _sessions;

    public AuthController(
        IRegistrationService registration,
        ILoginService login,
        JwtTokenService tokens,
        IAuthSessionService sessions)
    {
        _registration = registration;
        _login = login;
        _tokens = tokens;
        _sessions = sessions;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        Guid? id = await _registration.RegisterAsync(
            request, cancellationToken);

        return id is null
            ? Conflict(new { message = "Bu e-posta adresi zaten kayıtlı." })
            : StatusCode(StatusCodes.Status201Created, new { id });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        AuthenticatedUser? user = await _login.LoginAsync(
            request, cancellationToken);

        if (user is null)
            return Unauthorized(new { message = "E-posta veya parola hatalı." });

        var (token, expiresAtUtc) = _tokens.CreateToken(user);
        return Ok(new { accessToken = token, expiresAtUtc });
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
    public IActionResult Me()
    {
        string? id = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Ok(new { id });
    }
}