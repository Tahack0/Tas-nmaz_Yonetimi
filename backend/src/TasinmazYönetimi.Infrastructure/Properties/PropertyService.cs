using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using TasinmazYönetimi.Application.Properties;
using TasinmazYönetimi.Domain.Entities;
using TasinmazYönetimi.Infrastructure.Persistence;

namespace TasinmazYönetimi.Infrastructure.Properties;

public class PropertyService : IPropertyService
{
    private readonly ApplicationDbContext _db;
    private readonly GeometryFactory _geometryFactory =
        new(new PrecisionModel(), 4326);

    public PropertyService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PropertyResponse> CreateAsync(
        Guid userId,
        CreatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.PropertyType))
            throw new ArgumentException("Geçersiz taşınmaz türü.");

        var ring = request.ExteriorRing;

        if (ring is null || ring.Count < 4 || ring.Count > 10_000 ||
            ring.Any(point =>
                point is null ||
                point.Length != 2 ||
                !double.IsFinite(point[0]) ||
                !double.IsFinite(point[1]) ||
                point[0] < -180 || point[0] > 180 ||
                point[1] < -90 || point[1] > 90))
        {
            throw new ArgumentException(
                "Poligon en az 4 geçerli [boylam, enlem] noktası içermeli.");
        }

        Coordinate[] coordinates = ring
            .Select(point => new Coordinate(point[0], point[1]))
            .ToArray();

        if (!coordinates[0].Equals2D(coordinates[^1]))
            throw new ArgumentException("Poligonun ilk ve son noktası aynı olmalı.");

        Polygon polygon = _geometryFactory.CreatePolygon(coordinates);

        if (polygon.IsEmpty || !polygon.IsValid || polygon.Area == 0)
            throw new ArgumentException("Poligon geometrisi geçersiz.");

        var property = new Property
        {
            Id = Guid.NewGuid(),
            City = request.City.Trim(),
            District = request.District.Trim(),
            Neighborhood = request.Neighborhood.Trim(),
            ParcelNumber = request.ParcelNumber.Trim(),
            LotNumber = request.LotNumber.Trim(),
            Address = request.Address.Trim(),
            PropertyType = request.PropertyType,
            Geometry = polygon,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Properties.Add(property);
        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(property);
    }

    public async Task<IReadOnlyList<PropertyResponse>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var properties = await _db.Properties
            .AsNoTracking()
            .Where(property => property.UserId == userId)
            .OrderByDescending(property => property.CreatedAt)
            .ToListAsync(cancellationToken);

        return properties.Select(ToResponse).ToList();
    }

    public async Task<PropertyResponse?> GetByIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var property = await _db.Properties
            .AsNoTracking()
            .FirstOrDefaultAsync(
                property => property.Id == id && property.UserId == userId,
                cancellationToken);

        return property is null ? null : ToResponse(property);
    }

    private static PropertyResponse ToResponse(Property property) =>
        new(
            property.Id,
            property.City,
            property.District,
            property.Neighborhood,
            property.ParcelNumber,
            property.LotNumber,
            property.Address,
            property.PropertyType,
            property.Geometry.AsText(),
            property.CreatedAt);
}