using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
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

    public AuthController(
        IRegistrationService registration,
        ILoginService login,
        JwtTokenService tokens)
    {
        _registration = registration;
        _login = login;
        _tokens = tokens;
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

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        string? id = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Ok(new { id });
    }
}