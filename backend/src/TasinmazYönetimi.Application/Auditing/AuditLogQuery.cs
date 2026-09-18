using System.ComponentModel.DataAnnotations;
using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Application.Auditing;

public class AuditLogQuery
{
    public Guid? UserId { get; set; }

    [EnumDataType(typeof(LogStatus))]
    public LogStatus? Status { get; set; }

    [EnumDataType(typeof(OperationType))]
    public OperationType? OperationType { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }

    [Range(1, 1_000_000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}