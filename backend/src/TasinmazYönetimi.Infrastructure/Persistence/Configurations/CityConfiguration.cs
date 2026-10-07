using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TasinmazYönetimi.Domain.Entities;

namespace TasinmazYönetimi.Infrastructure.Persistence.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        // İl tablosunu ve zorunlu ad alanını tanımlar.
        builder.ToTable("cities");
        builder.HasKey(city => city.Id);
        builder.Property(city => city.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(city => city.Name).IsUnique();
    }
}