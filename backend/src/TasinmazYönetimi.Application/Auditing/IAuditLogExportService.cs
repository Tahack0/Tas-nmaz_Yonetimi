using TasinmazYönetimi.Application.Common;

namespace TasinmazYönetimi.Application.Auditing;

public interface IAuditLogExportService
{
    Task<ExportFileResponse> ExportExcelAsync(
        AuditLogExportQuery query,
        CancellationToken cancellationToken);

    Task<ExportFileResponse> ExportPdfAsync(
        AuditLogExportQuery query,
        CancellationToken cancellationToken);
}