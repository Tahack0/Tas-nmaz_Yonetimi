using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using TasinmazYönetimi.Application.SpatialAnalyses;
using TasinmazYönetimi.Domain.Entities;
using TasinmazYönetimi.Domain.Enums;
using TasinmazYönetimi.Infrastructure.Persistence;
using TasinmazYönetimi.Application.Auditing;

namespace TasinmazYönetimi.Infrastructure.SpatialAnalyses;

public sealed class SpatialAnalysisService : ISpatialAnalysisService
{
    private readonly ApplicationDbContext _db;
    private readonly SpatialGeometryCalculator _calculator;
    private readonly IAuditLogService _auditLogs;

    private void AddSuccessfulLog(Guid userId, string description) =>
    _auditLogs.Add(
        userId,
        LogStatus.Successful,
        OperationType.SpatialAnalysisPerformed,
        description);
    public SpatialAnalysisService(ApplicationDbContext db , IAuditLogService auditLogs)
    {
        _db = db;
        _calculator = new SpatialGeometryCalculator(db);
        _auditLogs = auditLogs;
    }

    // Üç poligonu doğrular; eski analiz sonuçlarıyla birlikte yeniler.
    public async Task<IReadOnlyList<SpatialAnalysisResponse>> SaveAsync(
        Guid userId,
        SaveSpatialGeometriesRequest request,
        CancellationToken cancellationToken)
    {
        var geometries = new[]
        {
            (SpatialGeometryLabel.A, _calculator.CreatePolygon(request.A)),
            (SpatialGeometryLabel.B, _calculator.CreatePolygon(request.B)),
            (SpatialGeometryLabel.C, _calculator.CreatePolygon(request.C))
        };

        var analyses = new List<SpatialAnalysis>();

        foreach (var (label, geometry) in geometries)
            analyses.Add(await CreateAnalysisAsync(
                userId, label, geometry, cancellationToken));

        var previous = await _db.SpatialAnalyses
            .Where(analysis => analysis.UserId == userId)
            .ToListAsync(cancellationToken);

        _db.SpatialAnalyses.RemoveRange(previous);
        _db.SpatialAnalyses.AddRange(analyses);
        // Geometriler ve audit kaydı aynı SaveChanges çağrısıyla kaydedilir.
        AddSuccessfulLog(userId, "A, B ve C analiz geometrileri kaydedildi.");
        await _db.SaveChangesAsync(cancellationToken);

        return analyses.Select(ToResponse).ToList();
    }

    // Auto-Select için kullanıcıya ait kayıtlı A/B/C geometrilerini getirir.
    public async Task<IReadOnlyList<SpatialAnalysisResponse>> GetSavedAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var analyses = await LoadInputsAsync(userId, cancellationToken);
        return analyses.Select(ToResponse).ToList();
    }

    // A∩B veya B∩A sonucunu veritabanına kaydetmeden döndürür.
    public async Task<SpatialAnalysisResponse?> IntersectAsync(
        Guid userId,
        bool reverseOrder,
        CancellationToken cancellationToken)
    {
        var inputs = await LoadInputsAsync(userId, cancellationToken);
        Geometry a = inputs[0].Geometry;
        Geometry b = inputs[1].Geometry;
        Geometry result = reverseOrder ? b.Intersection(a) : a.Intersection(b);

        if (result.IsEmpty)
        {
            // Kesişim bulunmaması, başarılı işlemin boş sonucudur.
            AddSuccessfulLog(userId, "Kesişim analizi tamamlandı: kesişim bulunamadı.");
            await _db.SaveChangesAsync(cancellationToken);
            return null;
        }

        double area = await _calculator.CalculateAreaAsync(
            result, cancellationToken);

        // Kesişim geometrisi kaydedilmez; yalnızca işlem logu kaydedilir.
        AddSuccessfulLog(
            userId,
            reverseOrder ? "B∩A kesişimi hesaplandı." : "A∩B kesişimi hesaplandı.");

        await _db.SaveChangesAsync(cancellationToken);
        // Label, işlemde ilk kullanılan giriş geometrisini belirtir.
        return new SpatialAnalysisResponse(
            reverseOrder ? SpatialGeometryLabel.B : SpatialGeometryLabel.A,
            result.AsText(),
            area);
    }

    // A∪B sonucunu D, A∪B∪C sonucunu E olarak kaydeder.
    public async Task<SpatialAnalysisResponse> UnionAsync(
        Guid userId,
        bool includeC,
        CancellationToken cancellationToken)
    {
        var inputs = await LoadInputsAsync(userId, cancellationToken);
        Geometry result = inputs[0].Geometry.Union(inputs[1].Geometry);

        if (includeC)
            result = result.Union(inputs[2].Geometry);

        var label = includeC ? SpatialGeometryLabel.E : SpatialGeometryLabel.D;
        var analysis = await CreateAnalysisAsync(
            userId, label, result, cancellationToken);

        await ReplaceResultAsync(analysis, cancellationToken);
        return ToResponse(analysis);
    }

    private async Task<List<SpatialAnalysis>> LoadInputsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var inputs = await _db.SpatialAnalyses.AsNoTracking()
            .Where(analysis => analysis.UserId == userId &&
                (analysis.Label == SpatialGeometryLabel.A ||
                 analysis.Label == SpatialGeometryLabel.B ||
                 analysis.Label == SpatialGeometryLabel.C))
            .OrderBy(analysis => analysis.Label)
            .ToListAsync(cancellationToken);

        if (inputs.Count != 3 ||
            inputs.Select(analysis => analysis.Label).Distinct().Count() != 3)
            throw new ArgumentException(
                "No saved geometries found. Please use Manual Draw.");

        return inputs;
    }

    private async Task<SpatialAnalysis> CreateAnalysisAsync(
        Guid userId,
        SpatialGeometryLabel label,
        Geometry geometry,
        CancellationToken cancellationToken)
    {
        geometry.SRID = 4326;

        return new SpatialAnalysis
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Label = label,
            Geometry = geometry,
            AreaSquareMeters = await _calculator.CalculateAreaAsync(
                geometry, cancellationToken),
            CreatedAt = DateTime.UtcNow
        };
    }

    // Aynı kullanıcı ve etiket için eski birleşim sonucunu değiştirir.
    private async Task ReplaceResultAsync(
        SpatialAnalysis analysis,
        CancellationToken cancellationToken)
    {
        var previous = await _db.SpatialAnalyses
            .Where(item => item.UserId == analysis.UserId &&
                item.Label == analysis.Label)
            .ToListAsync(cancellationToken);

        _db.SpatialAnalyses.RemoveRange(previous);
        _db.SpatialAnalyses.Add(analysis);
        // Birleşim sonucu ve audit kaydı birlikte kaydedilir.
        AddSuccessfulLog(
            analysis.UserId,
            analysis.Label == SpatialGeometryLabel.D
                ? "A∪B birleşimi D olarak kaydedildi."
                : "A∪B∪C birleşimi E olarak kaydedildi.");
        await _db.SaveChangesAsync(cancellationToken);
    }
    // İş kuralı hatasını mevcut audit servisiyle kaydeder.
    public async Task LogFailureAsync(
        Guid userId,
        string description,
        CancellationToken cancellationToken)
    {
        _auditLogs.Add(
            userId,
            LogStatus.Failed,
            OperationType.SpatialAnalysisPerformed,
            description);

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static SpatialAnalysisResponse ToResponse(
        SpatialAnalysis analysis) =>
        new(
            analysis.Label,
            analysis.Geometry.AsText(),
            analysis.AreaSquareMeters);
}