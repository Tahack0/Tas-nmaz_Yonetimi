using Microsoft.AspNetCore.Mvc;
using TasinmazYönetimi.Application.Auth;

namespace TasinmazYönetimi.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _registration;

    public RegistrationController(IRegistrationService registration)
    {
        _registration = registration;
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
}
