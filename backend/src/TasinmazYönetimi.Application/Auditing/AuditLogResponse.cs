using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Application.Auditing;

public record AuditLogResponse(
    Guid Id,
    Guid? UserId,
    LogStatus Status,
    OperationType OperationType,
    string Description,
    string IpAddress,
    DateTime CreatedAt);