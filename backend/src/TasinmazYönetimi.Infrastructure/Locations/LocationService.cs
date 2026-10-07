using Microsoft.EntityFrameworkCore;
using TasinmazYönetimi.Application.Locations;
using TasinmazYönetimi.Infrastructure.Persistence;

namespace TasinmazYönetimi.Infrastructure.Locations;

public class LocationService : ILocationService
{
    private readonly ApplicationDbContext _db;

    public LocationService(ApplicationDbContext db)
    {
        _db = db;
    }

    // Bütün illeri adlarına göre sıralar.
    public Task<List<LocationResponse>> GetCitiesAsync(
        CancellationToken cancellationToken) =>
        _db.Cities.AsNoTracking()
            .OrderBy(city => city.Name)
            .Select(city => new LocationResponse(city.Id, city.Name))
            .ToListAsync(cancellationToken);

    // Yalnızca seçilen ile bağlı ilçeleri getirir.
    public Task<List<LocationResponse>> GetDistrictsAsync(
        int cityId, CancellationToken cancellationToken) =>
        _db.Districts.AsNoTracking()
            .Where(district => district.CityId == cityId)
            .OrderBy(district => district.Name)
            .Select(district => new LocationResponse(
                district.Id, district.Name))
            .ToListAsync(cancellationToken);

    // Yalnızca seçilen ilçeye bağlı mahalleleri getirir.
    public Task<List<LocationResponse>> GetNeighborhoodsAsync(
        int districtId, CancellationToken cancellationToken) =>
        _db.Neighborhoods.AsNoTracking()
            .Where(neighborhood => neighborhood.DistrictId == districtId)
            .OrderBy(neighborhood => neighborhood.Name)
            .Select(neighborhood => new LocationResponse(
                neighborhood.Id, neighborhood.Name))
            .ToListAsync(cancellationToken);
}