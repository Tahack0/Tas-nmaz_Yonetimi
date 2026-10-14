using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasinmazYönetimi.Application.Auditing;
using TasinmazYönetimi.Application.Common;
using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/admin/audit-logs/export")]
public class AuditLogExportsController : ControllerBase
{
    private readonly IAuditLogExportService _auditLogExports;

    public AuditLogExportsController(IAuditLogExportService auditLogExports)
    {
        _auditLogExports = auditLogExports;
    }

    [HttpGet("excel")]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] AuditLogExportQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            ExportFileResponse result =
                await _auditLogExports.ExportExcelAsync(
                    query,
                    cancellationToken);

            return File(
                result.Content,
                result.ContentType,
                result.FileName);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] AuditLogExportQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            ExportFileResponse result =
                await _auditLogExports.ExportPdfAsync(
                    query,
                    cancellationToken);

            return File(
                result.Content,
                result.ContentType,
                result.FileName);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }
}