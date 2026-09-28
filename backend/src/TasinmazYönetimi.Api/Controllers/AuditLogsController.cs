using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasinmazYönetimi.Application.Auditing;
using TasinmazYönetimi.Application.Common;
using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/admin/audit-logs")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogs;
    private readonly IAuditLogExportService _auditLogExports;

    public AuditLogsController(
        IAuditLogService auditLogs,
        IAuditLogExportService auditLogExports)
    {
        _auditLogs = auditLogs;
        _auditLogExports = auditLogExports;
    }

    [HttpGet]
    public async Task<ActionResult<
        PagedResponse<AuditLogResponse>>> List(
        [FromQuery] AuditLogQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            PagedResponse<AuditLogResponse> result =
                await _auditLogs.ListAsync(
                    query,
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
    [HttpGet("export/excel")]
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

    [HttpGet("export/pdf")]
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