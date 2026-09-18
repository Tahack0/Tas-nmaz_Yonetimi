using Microsoft.EntityFrameworkCore;
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
                "Audit açıklaması boş olamaz.",
                nameof(description));
        }

        if (description.Length > 1000)
        {
            throw new ArgumentException(
                "Audit açıklaması 1000 karakteri geçemez.",
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

    public async Task<PagedResponse<AuditLogResponse>> ListAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken)
    {
        if (query.FromUtc.HasValue &&
            query.ToUtc.HasValue &&
            query.FromUtc.Value > query.ToUtc.Value)
        {
            throw new ArgumentException(
                "Başlangıç tarihi bitiş tarihinden büyük olamaz.");
        }

        IQueryable<AuditLog> auditLogs =
            _db.AuditLogs.AsNoTracking();

        if (query.UserId.HasValue)
        {
            auditLogs = auditLogs.Where(
                log => log.UserId == query.UserId.Value);
        }

        if (query.Status.HasValue)
        {
            auditLogs = auditLogs.Where(
                log => log.Status == query.Status.Value);
        }

        if (query.OperationType.HasValue)
        {
            auditLogs = auditLogs.Where(
                log =>
                    log.OperationType ==
                    query.OperationType.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Description))
        {
            string description = query.Description.Trim();

            auditLogs = auditLogs.Where(
                log => EF.Functions.ILike(
                    log.Description,
                    $"%{description}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.IpAddress))
        {
            string ipAddress = query.IpAddress.Trim();

            auditLogs = auditLogs.Where(
                log => log.IpAddress == ipAddress);
        }

        if (query.FromUtc.HasValue)
        {
            auditLogs = auditLogs.Where(
                log => log.CreatedAt >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            auditLogs = auditLogs.Where(
                log => log.CreatedAt <= query.ToUtc.Value);
        }

        int totalCount = await auditLogs.CountAsync(
            cancellationToken);

        int skip = (query.Page - 1) * query.PageSize;

        var items = await auditLogs
            .OrderByDescending(log => log.CreatedAt)
            .Skip(skip)
            .Take(query.PageSize)
            .Select(log => new AuditLogResponse(
                log.Id,
                log.UserId,
                log.Status,
                log.OperationType,
                log.Description,
                log.IpAddress,
                log.CreatedAt))
            .ToListAsync(cancellationToken);

        int totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(
                totalCount / (double)query.PageSize);

        return new PagedResponse<AuditLogResponse>(
            items,
            query.Page,
            query.PageSize,
            totalCount,
            totalPages);
    }
}