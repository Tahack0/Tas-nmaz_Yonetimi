using Microsoft.AspNetCore.Mvc;
using TasinmazYönetimi.Api.Security;
using TasinmazYönetimi.Application.Auth;

namespace TasinmazYönetimi.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ILoginService _login;
    private readonly JwtTokenService _tokens;

    public AuthController(ILoginService login, JwtTokenService tokens)
    {
        _login = login;
        _tokens = tokens;
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
}
