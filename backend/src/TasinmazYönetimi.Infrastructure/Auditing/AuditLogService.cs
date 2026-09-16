using TasinmazYönetimi.Application.Auditing;
using TasinmazYönetimi.Application.Common;
using TasinmazYönetimi.Domain.Entities;
using TasinmazYönetimi.Domain.Enums;
using TasinmazYönetimi.Infrastructure.Persistence;

namespace TasinmazYönetimi.Infrastructure.Auditing;

public sealed class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentRequestContext _requestContext;

    public AuditLogService(
        ApplicationDbContext db,
        ICurrentRequestContext requestContext)
    {
        _db = db;
        _requestContext = requestContext;
    }

    public void Add(
        Guid? userId,
        LogStatus status,
        OperationType operationType,
        string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "Audit aciklamasi boş olamaz.",
                nameof(description));
        }

        if (description.Length > 1000)
        {
            throw new ArgumentException(
                "Audit aciklamasi 1000 karakteri geçemez.",
                nameof(description));
        }

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = status,
            OperationType = operationType,
            Description = description,
            IpAddress = _requestContext.IpAddress,
            CreatedAt = DateTime.UtcNow
        };

        _db.AuditLogs.Add(auditLog);
    }
}