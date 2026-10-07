using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasinmazYönetimi.Application.SpatialAnalyses;

namespace TasinmazYönetimi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/spatial-analyses")]
public sealed class SpatialAnalysesController : ControllerBase
{
    private readonly ISpatialAnalysisService _analyses;

    public SpatialAnalysesController(ISpatialAnalysisService analyses)
    {
        _analyses = analyses;
    }

    // Manuel çizilen A/B/C geometrilerini kaydeder.
    [HttpPost("geometries")]
    public Task<IActionResult> Save(
        [FromBody] SaveSpatialGeometriesRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(userId =>
            _analyses.SaveAsync(userId, request, cancellationToken));

    // Auto-Select için kayıtlı A/B/C geometrilerini getirir.
    [HttpGet("geometries")]
    public Task<IActionResult> GetSaved(
        CancellationToken cancellationToken) =>
        ExecuteAsync(userId =>
            _analyses.GetSavedAsync(userId, cancellationToken));

    // reverseOrder=false: A∩B, true: B∩A.
    [HttpGet("intersection")]
    public Task<IActionResult> Intersect(
        CancellationToken cancellationToken,
        [FromQuery] bool reverseOrder = false) =>
        ExecuteAsync(userId =>
            _analyses.IntersectAsync(
                userId, reverseOrder, cancellationToken));

    // includeC=false: A∪B (D), true: A∪B∪C (E).
    [HttpPost("union")]
    public Task<IActionResult> Union(
        CancellationToken cancellationToken,
        [FromQuery] bool includeC = false) =>
        ExecuteAsync(userId =>
            _analyses.UnionAsync(userId, includeC, cancellationToken));

    private async Task<IActionResult> ExecuteAsync<T>(
        Func<Guid, Task<T>> operation)
    {
        if (!TryGetUserId(out Guid userId))
            return Unauthorized();

        try
        {
            T result = await operation(userId);

            return result is null
                ? Ok(new { message = "No intersection found." })
                : Ok(result);
        }
        catch (ArgumentException exception)
        {
            // İstek yolu analizi
            string description =
                $"Alan analizi başarısız ({Request.Path}): {exception.Message}";

            await _analyses.LogFailureAsync(
                userId,
                description,
                HttpContext.RequestAborted);

            return BadRequest(new { message = exception.Message });
        }
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub"),
            out userId);
}