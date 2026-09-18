using TasinmazYönetimi.Application.Common;
using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Application.Properties;

public interface IPropertyExportService
{
    Task<ExportFileResponse> ExportExcelAsync(
        Guid userId,
        UserRole userRole,
        PropertyExportQuery query,
        CancellationToken cancellationToken);

    Task<ExportFileResponse> ExportPdfAsync(
        Guid userId,
        UserRole userRole,
        PropertyExportQuery query,
        CancellationToken cancellationToken);
}