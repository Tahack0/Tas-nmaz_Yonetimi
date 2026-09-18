using System.ComponentModel.DataAnnotations;
using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Application.Properties;

public class PropertyExportQuery
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
}