using ClosedXML.Excel;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using TasinmazYönetimi.Application.Auditing;
using TasinmazYönetimi.Application.Properties;
using TasinmazYönetimi.Domain.Entities;
using TasinmazYönetimi.Domain.Enums;
using TasinmazYönetimi.Infrastructure.Persistence;
using NetTopologySuite;

namespace TasinmazYönetimi.Infrastructure.Properties;


public sealed class PropertyImportService : IPropertyImportService
{
    private const string ImportErrorMessage =
        "Import failed. Please check the file format and data.";

    private static readonly string[] RequiredHeaders =
    [
        "City",
        "District",
        "Neighborhood",
        "Parcel Number",
        "Lot Number",
        "Address",
        "Property Type",
        "Coordinates"
    ];

    private readonly ApplicationDbContext _db;
    private readonly IAuditLogService _auditLogs;

    public PropertyImportService(
        ApplicationDbContext db,
        IAuditLogService auditLogs)
    {
        _db = db;
        _auditLogs = auditLogs;
    }

    public async Task<PropertyImportResponse> ImportAsync(
        Guid userId,
        Stream fileStream,
        CancellationToken cancellationToken)
    {
        try
        {
            List<Property> properties =
                ReadAndValidateProperties(userId, fileStream);

            _db.Properties.AddRange(properties);

            _auditLogs.Add(
                userId,
                LogStatus.Successful,
                OperationType.PropertyImported,
                $"{properties.Count} taşınmaz Excel dosyasından içe aktarıldı.");

            await _db.SaveChangesAsync(cancellationToken);

            return new PropertyImportResponse(
                properties.Count,
                "Properties imported successfully.");
        }
        catch (Exception exception) when (
            exception is ArgumentException ||
            exception is FormatException ||
            exception is InvalidDataException)
        {
            _auditLogs.Add(
                userId,
                LogStatus.Failed,
                OperationType.PropertyImported,
                "Taşınmaz Excel içe aktarma işlemi başarısız oldu.");

            await _db.SaveChangesAsync(cancellationToken);

            throw new ArgumentException(
                ImportErrorMessage,
                exception);
        }
    }

    private List<Property> ReadAndValidateProperties(
        Guid userId,
        Stream fileStream)
    {
        if (fileStream is null || !fileStream.CanRead)
            throw new InvalidDataException();

        using var workbook = new XLWorkbook(fileStream);

        IXLWorksheet worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException();

        ValidateHeaders(worksheet);

        int lastRowNumber =
            worksheet.LastRowUsed()?.RowNumber() ?? 1;

        if (lastRowNumber < 2)
            throw new InvalidDataException();

        var properties = new List<Property>();

        for (int rowNumber = 2;
             rowNumber <= lastRowNumber;
             rowNumber++)
        {
            if (worksheet.Row(rowNumber).IsEmpty())
                continue;

            string city = ReadRequiredCell(
                worksheet, rowNumber, 1, 100);

            string district = ReadRequiredCell(
                worksheet, rowNumber, 2, 100);

            string neighborhood = ReadRequiredCell(
                worksheet, rowNumber, 3, 150);

            string parcelNumber = ReadRequiredCell(
                worksheet, rowNumber, 4, 50);

            string lotNumber = ReadRequiredCell(
                worksheet, rowNumber, 5, 50);

            string address = ReadRequiredCell(
                worksheet, rowNumber, 6, 500);

            PropertyType propertyType = ParsePropertyType(
                worksheet.Cell(rowNumber, 7).GetString());

            Polygon polygon = ParsePolygon(
                worksheet.Cell(rowNumber, 8).GetString());

            properties.Add(new Property
            {
                Id = Guid.NewGuid(),
                City = city,
                District = district,
                Neighborhood = neighborhood,
                ParcelNumber = parcelNumber,
                LotNumber = lotNumber,
                Address = address,
                PropertyType = propertyType,
                Geometry = polygon,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (properties.Count == 0)
            throw new InvalidDataException();

        return properties;
    }

    private static void ValidateHeaders(
        IXLWorksheet worksheet)
    {
        for (int columnNumber = 1;
             columnNumber <= RequiredHeaders.Length;
             columnNumber++)
        {
            string actualHeader = worksheet
                .Cell(1, columnNumber)
                .GetString()
                .Trim();

            if (!string.Equals(
                    actualHeader,
                    RequiredHeaders[columnNumber - 1],
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException();
            }
        }
    }

    private static string ReadRequiredCell(
        IXLWorksheet worksheet,
        int rowNumber,
        int columnNumber,
        int maximumLength)
    {
        string value = worksheet
            .Cell(rowNumber, columnNumber)
            .GetString()
            .Trim();

        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > maximumLength)
        {
            throw new InvalidDataException();
        }

        return value;
    }

    private static PropertyType ParsePropertyType(
        string value)
    {
        if (!Enum.TryParse(
                value.Trim(),
                ignoreCase: true,
                out PropertyType propertyType) ||
            !Enum.IsDefined(propertyType))
        {
            throw new InvalidDataException();
        }

        return propertyType;
    }

    private static Polygon ParsePolygon(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException();

        var reader = new WKTReader(NtsGeometryServices.Instance);

        Geometry geometry;

        try
        {
            geometry = reader.Read(value.Trim());
        }
        catch
        {
            throw new InvalidDataException();
        }

        if (geometry is not Polygon polygon)
            throw new InvalidDataException();

        Coordinate[] coordinates =
            polygon.ExteriorRing.Coordinates;

        if (coordinates.Length != 5 ||
            !coordinates[0].Equals2D(coordinates[^1]))
        {
            throw new InvalidDataException();
        }

        int distinctPointCount = coordinates
            .Take(4)
            .DistinctBy(coordinate =>
                (coordinate.X, coordinate.Y))
            .Count();

        if (distinctPointCount != 4 ||
            coordinates.Any(coordinate =>
                !double.IsFinite(coordinate.X) ||
                !double.IsFinite(coordinate.Y) ||
                coordinate.X < -180 ||
                coordinate.X > 180 ||
                coordinate.Y < -90 ||
                coordinate.Y > 90))
        {
            throw new InvalidDataException();
        }

        polygon.SRID = 4326;

        if (polygon.IsEmpty ||
            !polygon.IsValid ||
            polygon.Area == 0)
        {
            throw new InvalidDataException();
        }

        return polygon;
    }
}