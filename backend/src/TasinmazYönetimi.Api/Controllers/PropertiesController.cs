using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasinmazYönetimi.Application.Properties;
using TasinmazYönetimi.Domain.Enums;
using TasinmazYönetimi.Application.Common;

namespace TasinmazYönetimi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/properties")]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _properties;
    private readonly IPropertyExportService _propertyExports;
    private readonly IPropertyImportService _propertyImport;

    public PropertiesController(IPropertyService properties, IPropertyExportService propertyExports , IPropertyImportService propertyImport)
    {
        _properties = properties;
        _propertyExports = propertyExports;
        _propertyImport = propertyImport;
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
    [FromQuery] PropertyQuery query,
    CancellationToken cancellationToken)
{
    if (!TryGetUserId(out Guid userId))
        return Unauthorized();

    if (!TryGetUserRole(out UserRole userRole))
        return Forbid();

    var result = await _properties.ListAsync(
        userId,
        userRole,
        query,
        cancellationToken);

    return Ok(result);
}
[HttpGet("export/excel")]
public async Task<IActionResult> ExportExcel(
    [FromQuery] PropertyExportQuery query,
    CancellationToken cancellationToken)
{
    if (!TryGetUserId(out Guid userId))
        return Unauthorized();

    if (!TryGetUserRole(out UserRole userRole))
        return Forbid();

    ExportFileResponse result =
        await _propertyExports.ExportExcelAsync(
            userId,
            userRole,
            query,
            cancellationToken);

    return File(
        result.Content,
        result.ContentType,
        result.FileName);
}

[HttpGet("export/pdf")]
public async Task<IActionResult> ExportPdf(
    [FromQuery] PropertyExportQuery query,
    CancellationToken cancellationToken)
{
    if (!TryGetUserId(out Guid userId))
        return Unauthorized();

    if (!TryGetUserRole(out UserRole userRole))
        return Forbid();

    ExportFileResponse result =
        await _propertyExports.ExportPdfAsync(
            userId,
            userRole,
            query,
            cancellationToken);

    return File(
        result.Content,
        result.ContentType,
        result.FileName);
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

    [HttpPost("import/excel")]
    [Authorize(Roles = nameof(UserRole.User))]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportExcel(
         IFormFile file,
         CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId))
            return Unauthorized();

        if (file is null ||
            file.Length == 0 ||
            !string.Equals(
                Path.GetExtension(file.FileName),
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "Import failed. Please check the file format and data."
            });
        }

        try
        {
            await using Stream fileStream =
                file.OpenReadStream();

            PropertyImportResponse result =
                await _propertyImport.ImportAsync(
                    userId,
                    fileStream,
                    cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
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