using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using TasinmazYönetimi.Application.Auditing;
using TasinmazYönetimi.Application.Properties;
using TasinmazYönetimi.Domain.Entities;
using TasinmazYönetimi.Domain.Enums;
using TasinmazYönetimi.Infrastructure.Persistence;
using TasinmazYönetimi.Application.Common;


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
                $"Tasinmaz olusturuldu. Tasinmaz Id: {property.Id}.");

            await _db.SaveChangesAsync(cancellationToken);

            return ToResponse(property);
        }
        catch (ArgumentException)
        {
            await AddFailedAuditAsync(
                userId,
                OperationType.PropertyCreated,
                "Tasinmaz olusturulamadi.",
                cancellationToken);

            throw;
        }
    }

    public async Task<PagedResponse<PropertyResponse>> ListAsync(
        Guid userId,
    UserRole userRole,
    PropertyQuery query,
    CancellationToken cancellationToken)
    {
        IQueryable<Property> propertiesQuery =
            _db.Properties.AsNoTracking();

        if (userRole == UserRole.Admin)
        {
            if (query.UserId.HasValue)
            {
                propertiesQuery = propertiesQuery.Where(property =>
                    property.UserId == query.UserId.Value);
            }
        }
        else
        {
            propertiesQuery = propertiesQuery.Where(property =>
                property.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            string city = query.City.Trim();

            propertiesQuery = propertiesQuery.Where(property =>
                EF.Functions.ILike(
                    property.City,
                    $"%{city}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.District))
        {
            string district = query.District.Trim();

            propertiesQuery = propertiesQuery.Where(property =>
                EF.Functions.ILike(
                    property.District,
                    $"%{district}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Neighborhood))
        {
            string neighborhood =
                query.Neighborhood.Trim();

            propertiesQuery = propertiesQuery.Where(property =>
                EF.Functions.ILike(
                    property.Neighborhood,
                    $"%{neighborhood}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.ParcelNumber))
        {
            string parcelNumber =
                query.ParcelNumber.Trim();

            propertiesQuery = propertiesQuery.Where(property =>
                EF.Functions.ILike(
                    property.ParcelNumber,
                    $"%{parcelNumber}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.LotNumber))
        {
            string lotNumber =
                query.LotNumber.Trim();

            propertiesQuery = propertiesQuery.Where(property =>
                EF.Functions.ILike(
                    property.LotNumber,
                    $"%{lotNumber}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Address))
        {
            string address = query.Address.Trim();

            propertiesQuery = propertiesQuery.Where(property =>
                EF.Functions.ILike(
                    property.Address,
                    $"%{address}%"));
        }

        if (query.PropertyType.HasValue)
        {
            propertiesQuery = propertiesQuery.Where(property =>
                property.PropertyType ==
                query.PropertyType.Value);
        }

        int totalCount =
            await propertiesQuery.CountAsync(cancellationToken);

        var properties = await propertiesQuery
            .OrderByDescending(property =>
                property.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        int totalPages = (int)Math.Ceiling(
            totalCount / (double)query.PageSize);

        return new PagedResponse<PropertyResponse>(properties.Select(ToResponse).ToList(), query.Page, query.PageSize, totalCount, totalPages);
    }

    public async Task<PropertyResponse?> GetByIdAsync(
         Guid userId,
    UserRole userRole,
    Guid id,
    CancellationToken cancellationToken)
    {
        IQueryable<Property> query = _db.Properties.AsNoTracking().Where(property => property.Id == id);

        if (userRole != UserRole.Admin)
        {
            query = query.Where(property => property.UserId == userId);
        }

        var property = await query.FirstOrDefaultAsync(cancellationToken);

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
        var property = await _db.Properties.FirstOrDefaultAsync(property => property.Id == id && property.UserId == userId, cancellationToken);
        if (property is null)
        {
            await AddFailedAuditAsync(
                userId,
                OperationType.PropertyUpdated,
                $"Tasinmaz güncellenemedi. Tasinmaz Id: {id}.",
                cancellationToken);

            return null;
        }

        try
        {
            ValidatePropertyType(request.PropertyType);

            Polygon polygon = CreatePolygon(request.ExteriorRing);

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
                $"Tasinmaz güncellendi. Tasinmaz Id: {property.Id}.");

            await _db.SaveChangesAsync(cancellationToken);

            return ToResponse(property);
        }
        catch (ArgumentException)
        {
            await AddFailedAuditAsync(
                userId,
                OperationType.PropertyUpdated,
                $"Tasinmaz güncellenemedi. Tasinmaz Id: {id}.",
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
                $"Tasinmaz silinemedi. Tasinmaz Id: {id}.",
                cancellationToken);

            return false;
        }

        _db.Properties.Remove(property);

        _auditLogs.Add(
            userId,
            LogStatus.Successful,
            OperationType.PropertyDeleted,
            $"Tasinmaz silindi. Tasinmaz Id: {property.Id}.");

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
        if (ring is null || ring.Count != 5)
        {
            throw new ArgumentException(
                "Poligon 4 nokta ve başlangıç noktasının tekrarıyla toplam 5 koordinat içermeli.");
        }

        if (ring.Any(point =>
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
                "Poligon geçerli [boylam, enlem] koordinatları içermeli.");
        }

        Coordinate[] coordinates = ring
            .Select(point =>
                new Coordinate(point[0], point[1]))
            .ToArray();

        if (!coordinates[0].Equals2D(coordinates[^1]))
        {
            throw new ArgumentException(
                "Poligonun ilk ve son noktası aynı olmalı.");
        }

        int distinctPointCount = coordinates
            .Take(4)
            .Select(coordinate =>
                (coordinate.X, coordinate.Y))
            .Distinct()
            .Count();

        if (distinctPointCount != 4)
        {
            throw new ArgumentException(
                "Poligonun seçilen 4 noktası birbirinden farklı olmalı.");
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
                "Geçersiz taşinmaz türü.");
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