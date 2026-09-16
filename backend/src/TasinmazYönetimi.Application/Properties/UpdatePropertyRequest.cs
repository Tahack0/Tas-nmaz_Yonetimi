using System.ComponentModel.DataAnnotations;
using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Application.Properties;

public sealed class UpdatePropertyRequest
{
    [Required]
    [MaxLength(100)]
    public string City { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string District { get; init; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Neighborhood { get; init; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ParcelNumber { get; init; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LotNumber { get; init; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Address { get; init; } = string.Empty;

    [EnumDataType(typeof(PropertyType))]
    public PropertyType PropertyType { get; init; }

    [Required]
    public List<double[]> ExteriorRing { get; init; } = new();
}