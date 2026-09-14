using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TasinmazYönetimi.Domain.Entities;

namespace TasinmazYönetimi.Infrastructure.Persistence.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("properties");

        builder.HasKey(property => property.Id);

        builder.Property(property => property.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(property => property.District)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(property => property.Neighborhood)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(property => property.ParcelNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(property => property.LotNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(property => property.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(property => property.PropertyType)
            .IsRequired();

        builder.Property(property => property.Geometry)
            .IsRequired()
            .HasColumnType("geometry (polygon, 4326)");

        builder.Property(property => property.CreatedAt)
            .IsRequired();

        builder.HasIndex(property => property.UserId);

        builder.HasIndex(property => property.Geometry)
            .HasMethod("gist");

        builder.HasMany(property => property.MediaFiles)
            .WithOne(media => media.Property)
            .HasForeignKey(media => media.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
