using TasinmazYönetimi.Application.Common;
using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Application.Auditing;

public interface IAuditLogService
{
    void Add(
        Guid? userId,
        LogStatus status,
        OperationType operationType,
        string description);

    Task<PagedResponse<AuditLogResponse>> ListAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken);
}