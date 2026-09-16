using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using TasinmazYönetimi.Application.Auditing;
using TasinmazYönetimi.Application.Properties;
using TasinmazYönetimi.Domain.Entities;
using TasinmazYönetimi.Domain.Enums;
using TasinmazYönetimi.Infrastructure.Persistence;

namespace TasinmazYönetimi.Infrastructure.Properties;

public class PropertyService : IPropertyService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogService _auditLogs;

    private readonly GeometryFactory _geometryFactory =
        new(new PrecisionModel(), 4326);

    public PropertyService(
        ApplicationDbContext db,
        IAuditLogService auditLogs)
    {
        _db = db;
        _auditLogs = auditLogs;
    }

    public async Task<PropertyResponse> CreateAsync(
        Guid userId,
        CreatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidatePropertyType(request.PropertyType);

            Polygon polygon =
                CreatePolygon(request.ExteriorRing);

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

            _auditLogs.Add(
                userId,
                LogStatus.Successful,
                OperationType.PropertyCreated,
                $"Taşınmaz oluşturuldu. Taşınmaz Id: {property.Id}.");

            await _db.SaveChangesAsync(cancellationToken);

            return ToResponse(property);
        }
        catch (ArgumentException)
        {
            await AddFailedAuditAsync(
                userId,
                OperationType.PropertyCreated,
                "Taşınmaz oluşturulamadı.",
                cancellationToken);

            throw;
        }
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

        return properties
            .Select(ToResponse)
            .ToList();
    }

    public async Task<PropertyResponse?> GetByIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var property = await _db.Properties
            .AsNoTracking()
            .FirstOrDefaultAsync(
                property =>
                    property.Id == id &&
                    property.UserId == userId,
                cancellationToken);

        return property is null
            ? null
            : ToResponse(property);
    }

    public async Task<PropertyResponse?> UpdateAsync(
        Guid userId,
        Guid id,
        UpdatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        var property = await _db.Properties
            .FirstOrDefaultAsync(
                property =>
                    property.Id == id &&
                    property.UserId == userId,
                cancellationToken);

        if (property is null)
        {
            await AddFailedAuditAsync(
                userId,
                OperationType.PropertyUpdated,
                $"Taşınmaz güncellenemedi. Taşınmaz Id: {id}.",
                cancellationToken);

            return null;
        }

        try
        {
            ValidatePropertyType(request.PropertyType);

            Polygon polygon =
                CreatePolygon(request.ExteriorRing);

            property.City = request.City.Trim();
            property.District = request.District.Trim();
            property.Neighborhood = request.Neighborhood.Trim();
            property.ParcelNumber = request.ParcelNumber.Trim();
            property.LotNumber = request.LotNumber.Trim();
            property.Address = request.Address.Trim();
            property.PropertyType = request.PropertyType;
            property.Geometry = polygon;
            property.UpdatedAt = DateTime.UtcNow;

            _auditLogs.Add(
                userId,
                LogStatus.Successful,
                OperationType.PropertyUpdated,
                $"Taşınmaz güncellendi. Taşınmaz Id: {property.Id}.");

            await _db.SaveChangesAsync(cancellationToken);

            return ToResponse(property);
        }
        catch (ArgumentException)
        {
            await AddFailedAuditAsync(
                userId,
                OperationType.PropertyUpdated,
                $"Taşınmaz güncellenemedi. Taşınmaz Id: {id}.",
                cancellationToken);

            throw;
        }
    }

    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var property = await _db.Properties
            .FirstOrDefaultAsync(
                property =>
                    property.Id == id &&
                    property.UserId == userId,
                cancellationToken);

        if (property is null)
        {
            await AddFailedAuditAsync(
                userId,
                OperationType.PropertyDeleted,
                $"Taşınmaz silinemedi. Taşınmaz Id: {id}.",
                cancellationToken);

            return false;
        }

        _db.Properties.Remove(property);

        _auditLogs.Add(
            userId,
            LogStatus.Successful,
            OperationType.PropertyDeleted,
            $"Taşınmaz silindi. Taşınmaz Id: {property.Id}.");

        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task AddFailedAuditAsync(
        Guid userId,
        OperationType operationType,
        string description,
        CancellationToken cancellationToken)
    {
        _auditLogs.Add(
            userId,
            LogStatus.Failed,
            operationType,
            description);

        await _db.SaveChangesAsync(cancellationToken);
    }

    private Polygon CreatePolygon(
        List<double[]> ring)
    {
        if (ring is null ||
            ring.Count < 4 ||
            ring.Count > 10_000 ||
            ring.Any(point =>
                point is null ||
                point.Length != 2 ||
                !double.IsFinite(point[0]) ||
                !double.IsFinite(point[1]) ||
                point[0] < -180 ||
                point[0] > 180 ||
                point[1] < -90 ||
                point[1] > 90))
        {
            throw new ArgumentException(
                "Poligon en az 4 geçerli [boylam, enlem] noktası içermeli.");
        }

        Coordinate[] coordinates = ring
            .Select(point =>
                new Coordinate(point[0], point[1]))
            .ToArray();

        if (!coordinates[0]
                .Equals2D(coordinates[^1]))
        {
            throw new ArgumentException(
                "Poligonun ilk ve son noktası aynı olmalı.");
        }

        Polygon polygon =
            _geometryFactory.CreatePolygon(coordinates);

        if (polygon.IsEmpty ||
            !polygon.IsValid ||
            polygon.Area == 0)
        {
            throw new ArgumentException(
                "Poligon geometrisi geçersiz.");
        }

        return polygon;
    }

    private static void ValidatePropertyType(
        PropertyType propertyType)
    {
        if (!Enum.IsDefined(propertyType))
        {
            throw new ArgumentException(
                "Geçersiz taşınmaz türü.");
        }
    }

    private static PropertyResponse ToResponse(
        Property property) =>
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