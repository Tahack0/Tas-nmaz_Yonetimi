using NetTopologySuite.Geometries;
using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Domain.Entities;

public class Property
{
    public Guid Id { get; set; }

    public string City { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public string Neighborhood { get; set; } = string.Empty;

    public string ParcelNumber { get; set; } = string.Empty;

    public string LotNumber { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public PropertyType PropertyType { get; set; }

    public Polygon Geometry { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public ICollection<PropertyMedia> MediaFiles { get; set; }
        = new List<PropertyMedia>();

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}