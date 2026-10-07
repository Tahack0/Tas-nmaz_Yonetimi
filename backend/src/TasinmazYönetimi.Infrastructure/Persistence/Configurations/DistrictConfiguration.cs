using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TasinmazYönetimi.Domain.Entities;

namespace TasinmazYönetimi.Infrastructure.Persistence.Configurations;

public class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder.ToTable("districts");
        builder.HasKey(district => district.Id);
        builder.Property(district => district.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Aynı ilde aynı ilçe adının tekrarını engeller.
        builder.HasIndex(district => new
        {
            district.CityId,
            district.Name
        }).IsUnique();

        // İlçeyi bağlı olduğu ile bağlar.
        builder.HasOne(district => district.City)
            .WithMany()
            .HasForeignKey(district => district.CityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}