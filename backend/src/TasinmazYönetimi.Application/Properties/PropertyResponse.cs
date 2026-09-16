using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Application.Properties;

public record PropertyResponse(
    Guid Id,
    string City,
    string District,
    string Neighborhood,
    string ParcelNumber,
    string LotNumber,
    string Address,
    PropertyType PropertyType,
    string GeometryWkt,
    DateTime CreatedAt);