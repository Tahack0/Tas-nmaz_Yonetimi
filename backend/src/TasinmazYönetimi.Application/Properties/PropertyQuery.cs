using System.ComponentModel.DataAnnotations;
using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Application.Properties;

public class PropertyQuery
{
    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? District { get; set; }

    [StringLength(150)]
    public string? Neighborhood { get; set; }

    [StringLength(50)]
    public string? ParcelNumber { get; set; }

    [StringLength(50)]
    public string? LotNumber { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [EnumDataType(typeof(PropertyType))]
    public PropertyType? PropertyType { get; set; }

    public Guid? UserId { get; set; }

    [Range(1, 1_000_000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}