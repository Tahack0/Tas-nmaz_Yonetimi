using System.ComponentModel.DataAnnotations;
using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Application.Properties;

public class CreatePropertyRequest
{
    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string District { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Neighborhood { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string ParcelNumber { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string LotNumber { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Address { get; set; } = string.Empty;

    public PropertyType PropertyType { get; set; }
    [Required, MinLength(4)]
    public List<double[]> ExteriorRing { get; set; } = new();
}