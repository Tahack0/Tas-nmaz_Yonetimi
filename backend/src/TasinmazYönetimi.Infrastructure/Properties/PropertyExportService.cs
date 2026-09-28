using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using TasinmazYönetimi.Application.Auditing;
using TasinmazYönetimi.Application.Common;
using TasinmazYönetimi.Application.Properties;
using TasinmazYönetimi.Domain.Entities;
using TasinmazYönetimi.Domain.Enums;
using TasinmazYönetimi.Infrastructure.Persistence;

namespace TasinmazYönetimi.Infrastructure.Properties;

public sealed class PropertyExportService : IPropertyExportService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogService _auditLogs;

    public PropertyExportService(
        ApplicationDbContext db,
        IAuditLogService auditLogs)
    {
        _db = db;
        _auditLogs = auditLogs;
    }

    public async Task<ExportFileResponse> ExportExcelAsync(
        Guid userId,
        UserRole userRole,
        PropertyExportQuery request,
        CancellationToken cancellationToken)
    {
        List<Property> properties = await GetPropertiesAsync(
            userId,
            userRole,
            request,
            cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Tasinmazlar");

        string[] headers =
        [
            "Id",
            "Kullanici Id",
            "Il",
            "Ilce",
            "Mahalle",
            "Parsel No",
            "Ada No",
            "Adres",
            "Tasinmaz Turu",
            "Geometri",
            "Olusturulma Tarihi",
            "Guncellenme Tarihi"
        ];

        for (int column = 1; column <= headers.Length; column++)
        {
            worksheet.Cell(1, column).Value = headers[column - 1];
        }

        var headerRange = worksheet.Range(
            1,
            1,
            1,
            headers.Length);

        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;

        int row = 2;

        foreach (Property property in properties)
        {
            worksheet.Cell(row, 1).Value = property.Id.ToString();
            worksheet.Cell(row, 2).Value = property.UserId.ToString();
            worksheet.Cell(row, 3).Value = property.City;
            worksheet.Cell(row, 4).Value = property.District;
            worksheet.Cell(row, 5).Value = property.Neighborhood;
            worksheet.Cell(row, 6).Value = property.ParcelNumber;
            worksheet.Cell(row, 7).Value = property.LotNumber;
            worksheet.Cell(row, 8).Value = property.Address;
            worksheet.Cell(row, 9).Value =
                property.PropertyType.ToString();
            worksheet.Cell(row, 10).Value =
                property.Geometry.AsText();
            worksheet.Cell(row, 11).Value = property.CreatedAt;
            worksheet.Cell(row, 12).Value = property.UpdatedAt;

            row++;
        }

        worksheet.Column(11).Style.DateFormat.Format =
            "dd.MM.yyyy HH:mm:ss";

        worksheet.Column(12).Style.DateFormat.Format =
            "dd.MM.yyyy HH:mm:ss";

        worksheet.SheetView.FreezeRows(1);
        worksheet.RangeUsed()?.SetAutoFilter();
        worksheet.Columns().AdjustToContents();

        worksheet.Column(8).Width = Math.Min(
            worksheet.Column(8).Width,
            50);

        worksheet.Column(10).Width = Math.Min(
            worksheet.Column(10).Width,
            60);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        await AddSuccessfulAuditAsync(
            userId,
            "Tasinmazlar Excel olarak disa aktarildi.",
            cancellationToken);

        return new ExportFileResponse(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"tasinmazlar_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
    }

    public async Task<ExportFileResponse> ExportPdfAsync(
        Guid userId,
        UserRole userRole,
        PropertyExportQuery request,
        CancellationToken cancellationToken)
    {
        List<Property> properties = await GetPropertiesAsync(
            userId,
            userRole,
            request,
            cancellationToken);

        var document = new Document();

        document.Info.Title = "Tasinmaz Listesi";
        document.Info.Subject = "Tasinmaz disa aktarma raporu";

        Style normalStyle = document.Styles["Normal"]
     ?? throw new InvalidOperationException("PDF normal stili oluşturulamadı.");

        normalStyle.Font.Name = "Arial";
        normalStyle.Font.Size = 9;

        Section section = document.AddSection();

        Paragraph title = section.AddParagraph("TASINMAZ LISTESI");
        title.Format.Font.Size = 16;
        title.Format.Font.Bold = true;
        title.Format.Alignment = ParagraphAlignment.Center;
        title.Format.SpaceAfter = Unit.FromCentimeter(0.5);

        Paragraph summary = section.AddParagraph(
            $"Kayit sayisi: {properties.Count} | " +
            $"Rapor tarihi: {DateTime.UtcNow:dd.MM.yyyy HH:mm:ss} UTC");

        summary.Format.Alignment = ParagraphAlignment.Center;
        summary.Format.SpaceAfter = Unit.FromCentimeter(0.5);

        foreach (Property property in properties)
        {
            Paragraph propertyTitle = section.AddParagraph(
                $"{property.City} / {property.District} / " +
                $"{property.Neighborhood}");

            propertyTitle.Format.Font.Size = 11;
            propertyTitle.Format.Font.Bold = true;
            propertyTitle.Format.SpaceBefore =
                Unit.FromCentimeter(0.3);
            propertyTitle.Format.SpaceAfter =
                Unit.FromCentimeter(0.1);

            AddField(section, "Id", property.Id.ToString());
            AddField(
                section,
                "Kullanici Id",
                property.UserId.ToString());

            AddField(section, "Il", property.City);
            AddField(section, "Ilce", property.District);
            AddField(section, "Mahalle", property.Neighborhood);
            AddField(
                section,
                "Parsel No",
                property.ParcelNumber);

            AddField(section, "Ada No", property.LotNumber);
            AddField(section, "Adres", property.Address);

            AddField(
                section,
                "Tasinmaz Turu",
                property.PropertyType.ToString());

            AddField(
                section,
                "Geometri",
                property.Geometry.AsText());

            AddField(
                section,
                "Olusturulma Tarihi",
                property.CreatedAt.ToString(
                    "dd.MM.yyyy HH:mm:ss"));

            AddField(
                section,
                "Guncellenme Tarihi",
                property.UpdatedAt?.ToString(
                    "dd.MM.yyyy HH:mm:ss") ?? "-");

            Paragraph separator = section.AddParagraph();
            separator.Format.Borders.Bottom.Width = 0.5;
            separator.Format.SpaceAfter =
                Unit.FromCentimeter(0.2);
        }

        var renderer = new PdfDocumentRenderer
        {
            Document = document
        };

        renderer.RenderDocument();

        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, false);

        await AddSuccessfulAuditAsync(
            userId,
            "Tasinmazlar PDF olarak disa aktarildi.",
            cancellationToken);

        return new ExportFileResponse(
            stream.ToArray(),
            "application/pdf",
            $"tasinmazlar_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
    }

    private async Task<List<Property>> GetPropertiesAsync(
        Guid userId,
        UserRole userRole,
        PropertyExportQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<Property> query =
            _db.Properties.AsNoTracking();

        if (userRole == UserRole.Admin)
        {
            if (request.UserId.HasValue)
            {
                query = query.Where(property =>
                    property.UserId == request.UserId.Value);
            }
        }
        else
        {
            query = query.Where(property =>
                property.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            string value = request.City.Trim();

            query = query.Where(property =>
                EF.Functions.ILike(
                    property.City,
                    $"%{value}%"));
        }

        if (!string.IsNullOrWhiteSpace(request.District))
        {
            string value = request.District.Trim();

            query = query.Where(property =>
                EF.Functions.ILike(
                    property.District,
                    $"%{value}%"));
        }

        if (!string.IsNullOrWhiteSpace(request.Neighborhood))
        {
            string value = request.Neighborhood.Trim();

            query = query.Where(property =>
                EF.Functions.ILike(
                    property.Neighborhood,
                    $"%{value}%"));
        }

        if (!string.IsNullOrWhiteSpace(request.ParcelNumber))
        {
            string value = request.ParcelNumber.Trim();

            query = query.Where(property =>
                EF.Functions.ILike(
                    property.ParcelNumber,
                    $"%{value}%"));
        }

        if (!string.IsNullOrWhiteSpace(request.LotNumber))
        {
            string value = request.LotNumber.Trim();

            query = query.Where(property =>
                EF.Functions.ILike(
                    property.LotNumber,
                    $"%{value}%"));
        }

        if (!string.IsNullOrWhiteSpace(request.Address))
        {
            string value = request.Address.Trim();

            query = query.Where(property =>
                EF.Functions.ILike(
                    property.Address,
                    $"%{value}%"));
        }

        if (request.PropertyType.HasValue)
        {
            query = query.Where(property =>
                property.PropertyType ==
                request.PropertyType.Value);
        }

        return await query
            .OrderByDescending(property => property.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private async Task AddSuccessfulAuditAsync(
        Guid userId,
        string description,
        CancellationToken cancellationToken)
    {
        _auditLogs.Add(
            userId,
            LogStatus.Successful,
            OperationType.PropertyExported,
            description);

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void AddField(
        Section section,
        string name,
        string value)
    {
        Paragraph paragraph = section.AddParagraph();

        FormattedText label = paragraph.AddFormattedText(
            $"{name}: ");

        label.Bold = true;
        paragraph.AddText(value);
        paragraph.Format.SpaceAfter = Unit.FromPoint(2);
    }
}