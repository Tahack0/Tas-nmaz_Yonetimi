using System.ComponentModel.DataAnnotations;

namespace TasinmazYönetimi.Application.SpatialAnalyses;

public sealed class SaveSpatialGeometriesRequest
{
    [Required, MinLength(4)]
    public List<double[]> A { get; init; } = new();

    [Required, MinLength(4)]
    public List<double[]> B { get; init; } = new();

    [Required, MinLength(4)]
    public List<double[]> C { get; init; } = new();
}