using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TasinmazYönetimi.Domain.Entities;
using MediaEntity = TasinmazYönetimi.Domain.Entities.PropertyMedia;

namespace TasinmazYönetimi.Infrastructure.Persistence.Configurations;

public class PropertyMediaConfiguration : IEntityTypeConfiguration<MediaEntity>
{
    public void Configure(EntityTypeBuilder<MediaEntity> builder)
    {
        builder.ToTable("property_media");
        builder.HasKey(media => media.Id);

        builder.Property(media => media.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(media => media.FilePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(media => media.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(media => media.FileSize).IsRequired();
        builder.Property(media => media.CreatedAt).IsRequired();

        builder.HasIndex(media => media.PropertyId);
    }
}