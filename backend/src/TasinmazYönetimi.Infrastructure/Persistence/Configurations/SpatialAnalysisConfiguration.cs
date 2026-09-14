using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TasinmazYönetimi.Domain.Entities;

namespace TasinmazYönetimi.Infrastructure.Persistence.Configurations;

public class SpatialAnalysisConfiguration : IEntityTypeConfiguration<SpatialAnalysis>
{
    public void Configure(EntityTypeBuilder<SpatialAnalysis> builder)
    {
        builder.ToTable("spatial_analyses");
        builder.HasKey(analysis => analysis.Id);

        builder.Property(analysis => analysis.Label).IsRequired();

        builder.Property(analysis => analysis.Geometry)
            .IsRequired()
            .HasColumnType("geometry(Geometry,4326)");

        builder.Property(analysis => analysis.AreaSquareMeters)
            .IsRequired();

        builder.Property(analysis => analysis.CreatedAt)
            .IsRequired();

        builder.HasIndex(analysis => analysis.UserId);
        builder.HasIndex(analysis => analysis.Geometry).HasMethod("gist");
    }
}