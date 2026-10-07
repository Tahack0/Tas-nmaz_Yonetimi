using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using TasinmazYönetimi.Infrastructure.Persistence;

namespace TasinmazYönetimi.Infrastructure.SpatialAnalyses;

internal sealed class SpatialGeometryCalculator
{
    private readonly ApplicationDbContext _db;
    private readonly GeometryFactory _factory = new(new PrecisionModel(), 4326);

    public SpatialGeometryCalculator(ApplicationDbContext db)
    {
        _db = db;
    }

    // Koordinatları kontrol edip geçerli bir poligon oluşturur.
    public Polygon CreatePolygon(List<double[]> points)
    {
        if (points is null || points.Count < 3)
            throw new ArgumentException("Poligon en az üç köşe içermelidir.");

        if (points.Any(point => !IsValidCoordinate(point)))
            throw new ArgumentException("Koordinatlar geçerli boylam ve enlem içermelidir.");

        var coordinates = points
            .Select(point => new Coordinate(point[0], point[1]))
            .ToList();

        if (!coordinates[0].Equals2D(coordinates[^1]))
            coordinates.Add(new Coordinate(coordinates[0]));

        var polygon = _factory.CreatePolygon(coordinates.ToArray());

        if (polygon.IsEmpty || !polygon.IsValid || polygon.Area <= 0)
            throw new ArgumentException("Poligon geçersiz veya kendi üzerine kesişiyor.");

        return polygon;
    }

    // PostGIS geography üzerinden alanı metrekare olarak hesaplar.
    public Task<double> CalculateAreaAsync(
        Geometry geometry,
        CancellationToken cancellationToken)
    {
        string wkt = geometry.AsText();

        return _db.Database.SqlQuery<double>(
            $"""
            SELECT ST_Area(
                ST_GeomFromText({wkt}, 4326)::geography
            ) AS "Value"
            """)
            .SingleAsync(cancellationToken);
    }

    private static bool IsValidCoordinate(double[]? point) =>
        point is { Length: 2 } &&
        double.IsFinite(point[0]) &&
        double.IsFinite(point[1]) &&
        point[0] is >= -180 and <= 180 &&
        point[1] is >= -90 and <= 90;
}