using System.ComponentModel.DataAnnotations;
using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Application.Auditing;

public class AuditLogExportQuery
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
}