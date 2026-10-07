using Microsoft.EntityFrameworkCore;
using TasinmazYönetimi.Domain.Entities;

namespace TasinmazYönetimi.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<User> Users => Set<User>();

    public DbSet<Property> Properties => Set<Property>();

    public DbSet<TasinmazYönetimi.Domain.Entities.PropertyMedia> PropertyMedia
    => Set<TasinmazYönetimi.Domain.Entities.PropertyMedia>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<SpatialAnalysis> SpatialAnalyses => Set<SpatialAnalysis>();
    // İl, ilçe ve mahalle tablolarına erişim sağlar.
    public DbSet<City> Cities => Set<City>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Neighborhood> Neighborhoods => Set<Neighborhood>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}