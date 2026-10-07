using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Application.SpatialAnalyses;

public sealed record SpatialAnalysisResponse(
    SpatialGeometryLabel Label,
    string GeometryWkt,
    double AreaSquareMeters);