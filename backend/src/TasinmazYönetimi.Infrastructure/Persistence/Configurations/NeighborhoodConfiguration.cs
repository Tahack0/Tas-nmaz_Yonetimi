using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TasinmazYönetimi.Domain.Entities;

namespace TasinmazYönetimi.Infrastructure.Persistence.Configurations;

public class NeighborhoodConfiguration
    : IEntityTypeConfiguration<Neighborhood>
{
    public void Configure(EntityTypeBuilder<Neighborhood> builder)
    {
        builder.ToTable("neighborhoods");
        builder.HasKey(neighborhood => neighborhood.Id);
        builder.Property(neighborhood => neighborhood.Name)
            .IsRequired()
            .HasMaxLength(150);

        // İlçeye göre mahalle sorgularını hızlandırır.
        builder.HasIndex(neighborhood => neighborhood.DistrictId);

        // Mahalleyi bağlı olduğu ilçeye bağlar.
        builder.HasOne(neighborhood => neighborhood.District)
            .WithMany()
            .HasForeignKey(neighborhood => neighborhood.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}