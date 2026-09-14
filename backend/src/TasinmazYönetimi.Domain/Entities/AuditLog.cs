
using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public User? User { get; set; }

    public LogStatus Status { get; set; }

    public OperationType OperationType { get; set; }

    public string Description { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}