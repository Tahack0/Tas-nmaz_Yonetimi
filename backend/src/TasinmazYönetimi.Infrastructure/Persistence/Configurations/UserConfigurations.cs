using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TasinmazYönetimi.Domain.Entities;

namespace TasinmazYönetimi.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.Property(user => user.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(user => user.PasswordSalt)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(user => user.Role)
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .IsRequired();

        builder.HasMany(user => user.Properties)
            .WithOne(property => property.User)
            .HasForeignKey(property => property.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.AuditLogs)
            .WithOne(log => log.User)
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(user => user.SpatialAnalyses)
            .WithOne(analysis => analysis.User)
            .HasForeignKey(analysis => analysis.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}