using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TasinmazYönetimi.Domain.Entities;

namespace TasinmazYönetimi.Infrastructure.Persistence.Configurations;

public class RevokedTokenConfiguration : IEntityTypeConfiguration<RevokedToken>
{
    public void Configure(EntityTypeBuilder<RevokedToken> builder)
    {
        builder.ToTable("revoked_tokens");
        builder.HasKey(token => token.TokenHash).HasName("pk_revoked_tokens");
        builder.Property(token => token.TokenHash).HasMaxLength(64);
        builder.HasIndex(token => token.ExpiresAtUtc);
    }
}
