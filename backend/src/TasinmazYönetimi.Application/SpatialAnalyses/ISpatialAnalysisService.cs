namespace TasinmazYönetimi.Application.SpatialAnalyses;

public interface ISpatialAnalysisService
{
    Task<IReadOnlyList<SpatialAnalysisResponse>> SaveAsync(
        Guid userId,
        SaveSpatialGeometriesRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SpatialAnalysisResponse>> GetSavedAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<SpatialAnalysisResponse?> IntersectAsync(
        Guid userId,
        bool reverseOrder,
        CancellationToken cancellationToken);

    Task<SpatialAnalysisResponse> UnionAsync(
        Guid userId,
        bool includeC,
        CancellationToken cancellationToken);

    Task LogFailureAsync(
    Guid userId,
    string description,
    CancellationToken cancellationToken);    
}