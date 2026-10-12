using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using TasinmazYönetimi.Application.Auditing;
using TasinmazYönetimi.Application.Common;
using TasinmazYönetimi.Domain.Entities;
using TasinmazYönetimi.Infrastructure.Persistence;

namespace TasinmazYönetimi.Infrastructure.Auditing;

public sealed class AuditLogExportService
    : IAuditLogExportService
{
    private readonly ApplicationDbContext _db;

    public AuditLogExportService(
        ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ExportFileResponse> ExportExcelAsync(
        AuditLogExportQuery query,
        CancellationToken cancellationToken)
    {
        List<AuditLog> logs = await GetLogsAsync(
            query,
            cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Audit Kayitlari");

        string[] headers =
        [
            "Id",
            "Kullanici Id",
            "Durum",
            "Islem Turu",
            "Aciklama",
            "IP Adresi",
            "Olusturulma Tarihi"
        ];

        for (int column = 1;
             column <= headers.Length;
             column++)
        {
            worksheet.Cell(1, column).Value =
                headers[column - 1];
        }

        var headerRange = worksheet.Range(
            1,
            1,
            1,
            headers.Length);

        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor =
            XLColor.DarkBlue;

        int row = 2;

        foreach (AuditLog log in logs)
        {
            worksheet.Cell(row, 1).Value =
                log.Id.ToString();

            worksheet.Cell(row, 2).Value =
                log.UserId?.ToString() ?? string.Empty;

            worksheet.Cell(row, 3).Value =
                log.Status.ToString();

            worksheet.Cell(row, 4).Value =
                log.OperationType.ToString();

            worksheet.Cell(row, 5).Value =
                log.Description;

            worksheet.Cell(row, 6).Value =
                log.IpAddress;

            worksheet.Cell(row, 7).Value =
                log.CreatedAt;

            row++;
        }

        worksheet.Column(7).Style.DateFormat.Format =
            "dd.MM.yyyy HH:mm:ss";

        worksheet.SheetView.FreezeRows(1);
        worksheet.RangeUsed()?.SetAutoFilter();
        worksheet.Columns().AdjustToContents();

        worksheet.Column(5).Width = Math.Min(
            worksheet.Column(5).Width,
            60);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExportFileResponse(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"audit_kayitlari_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
    }

    public async Task<ExportFileResponse> ExportPdfAsync(
        AuditLogExportQuery query,
        CancellationToken cancellationToken)
    {
        List<AuditLog> logs = await GetLogsAsync(
            query,
            cancellationToken);

        var document = new Document();

        document.Info.Title = "Audit Kayitlari";
        document.Info.Subject =
            "Audit kayitlari disa aktarma raporu";

        Style normalStyle = document.Styles["Normal"]
            ?? throw new InvalidOperationException(
                "PDF normal stili olusturulamadi.");

        normalStyle.Font.Name = "Arial";
        normalStyle.Font.Size = 9;

        Section section = document.AddSection();

        Paragraph title = section.AddParagraph(
            "AUDIT KAYITLARI");

        title.Format.Font.Size = 16;
        title.Format.Font.Bold = true;
        title.Format.Alignment =
            ParagraphAlignment.Center;

        title.Format.SpaceAfter =
            Unit.FromCentimeter(0.5);

        Paragraph summary = section.AddParagraph(
            $"Kayit sayisi: {logs.Count} | " +
            $"Rapor tarihi: " +
            $"{DateTime.UtcNow:dd.MM.yyyy HH:mm:ss} UTC");

        summary.Format.Alignment =
            ParagraphAlignment.Center;

        summary.Format.SpaceAfter =
            Unit.FromCentimeter(0.5);

        foreach (AuditLog log in logs)
        {
            Paragraph logTitle = section.AddParagraph(
                $"{log.OperationType} - {log.Status}");

            logTitle.Format.Font.Size = 11;
            logTitle.Format.Font.Bold = true;

            logTitle.Format.SpaceBefore =
                Unit.FromCentimeter(0.3);

            logTitle.Format.SpaceAfter =
                Unit.FromCentimeter(0.1);

            AddField(section, "Id", log.Id.ToString());

            AddField(
                section,
                "Kullanici Id",
                log.UserId?.ToString() ?? "-");

            AddField(
                section,
                "Durum",
                log.Status.ToString());

            AddField(
                section,
                "Islem Turu",
                log.OperationType.ToString());

            AddField(
                section,
                "Aciklama",
                log.Description);

            AddField(
                section,
                "IP Adresi",
                log.IpAddress);

            AddField(
                section,
                "Olusturulma Tarihi",
                log.CreatedAt.ToString(
                    "dd.MM.yyyy HH:mm:ss"));

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
        await renderer.PdfDocument.SaveAsync(stream, false);

        return new ExportFileResponse(
            stream.ToArray(),
            "application/pdf",
            $"audit_kayitlari_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
    }

    private async Task<List<AuditLog>> GetLogsAsync(
        AuditLogExportQuery query,
        CancellationToken cancellationToken)
    {
        if (query.FromUtc.HasValue &&
            query.ToUtc.HasValue &&
            query.FromUtc.Value > query.ToUtc.Value)
        {
            throw new ArgumentException(
                "Baslangic tarihi bitis tarihinden buyuk olamaz.");
        }

        IQueryable<AuditLog> logs =
            _db.AuditLogs.AsNoTracking();

        if (query.UserId.HasValue)
        {
            logs = logs.Where(log =>
                log.UserId == query.UserId.Value);
        }

        if (query.Status.HasValue)
        {
            logs = logs.Where(log =>
                log.Status == query.Status.Value);
        }

        if (query.OperationType.HasValue)
        {
            logs = logs.Where(log =>
                log.OperationType ==
                query.OperationType.Value);
        }

        if (!string.IsNullOrWhiteSpace(
                query.Description))
        {
            string description =
                query.Description.Trim();

            logs = logs.Where(log =>
                EF.Functions.ILike(
                    log.Description,
                    $"%{description}%"));
        }

        if (!string.IsNullOrWhiteSpace(
                query.IpAddress))
        {
            string ipAddress =
                query.IpAddress.Trim();

            logs = logs.Where(log =>
                log.IpAddress == ipAddress);
        }

        if (query.FromUtc.HasValue)
        {
            logs = logs.Where(log =>
                log.CreatedAt >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            logs = logs.Where(log =>
                log.CreatedAt <= query.ToUtc.Value);
        }

        return await logs
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private static void AddField(
        Section section,
        string name,
        string value)
    {
        Paragraph paragraph = section.AddParagraph();

        FormattedText label =
            paragraph.AddFormattedText($"{name}: ");

        label.Bold = true;
        paragraph.AddText(value);

        paragraph.Format.SpaceAfter =
            Unit.FromPoint(2);
    }
}