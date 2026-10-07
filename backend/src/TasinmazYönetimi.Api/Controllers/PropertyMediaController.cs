using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasinmazYönetimi.Application.PropertyMedia;
using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Api.Controllers;

[ApiController]
[Authorize(Roles = "User,Admin")]
[Route("api/properties/{propertyId:guid}/photos")]
public sealed class PropertyMediaController : ControllerBase
{
    private readonly IPropertyMediaService _media;
    public PropertyMediaController(IPropertyMediaService media) => _media = media;

    // Yalnız User yükleyebilir; 100 MB dosya sınırını servis kontrol eder.
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.User))]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(110 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 110 * 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid propertyId, [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out Guid userId, out _)) return Unauthorized();
        if (file is null) return BadRequest(new { message = "Fotoğraf seçin." });
        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _media.UploadAsync(userId, propertyId, file.FileName,
                stream, file.Length, cancellationToken);
            return result is null ? NotFound() : CreatedAtAction(nameof(GetFile),
                new { propertyId, mediaId = result.Id }, result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid propertyId, CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out Guid userId, out UserRole role)) return Unauthorized();
        var result = await _media.ListAsync(userId, role, propertyId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    // Fotoğrafı yalnız yetkili oturumlara döndürür; klasörü herkese açmaz.
    [HttpGet("{mediaId:guid}/file")]
    public async Task<IActionResult> GetFile(Guid propertyId, Guid mediaId,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out Guid userId, out UserRole role)) return Unauthorized();
        var result = await _media.OpenAsync(userId, role, propertyId, mediaId, cancellationToken);
        if (result is null) return NotFound();
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(result.Content, result.ContentType);
    }

    private bool TryGetIdentity(out Guid userId, out UserRole role)
    {
        role = default;
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub"), out userId) &&
            Enum.TryParse(User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role"),
                true, out role) && Enum.IsDefined(role);
    }
}
