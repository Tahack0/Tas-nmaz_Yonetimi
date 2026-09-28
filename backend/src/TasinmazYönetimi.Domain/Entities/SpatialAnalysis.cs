using NetTopologySuite.Geometries;
using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Domain.Entities;

public class SpatialAnalysis
{
    public Guid Id { get; set; }

    public SpatialGeometryLabel Label { get; set; }

    public Geometry Geometry { get; set; } = null!;

    public double AreaSquareMeters { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}