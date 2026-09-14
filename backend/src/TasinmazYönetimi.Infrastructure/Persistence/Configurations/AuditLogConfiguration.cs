using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TasinmazYönetimi.Domain.Entities;

namespace TasinmazYönetimi.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.Status).IsRequired();
        builder.Property(log => log.OperationType).IsRequired();

        builder.Property(log => log.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(log => log.IpAddress)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(log => log.CreatedAt).IsRequired();

        builder.HasIndex(log => log.UserId);
        builder.HasIndex(log => log.CreatedAt);
        builder.HasIndex(log => log.OperationType);
    }
}