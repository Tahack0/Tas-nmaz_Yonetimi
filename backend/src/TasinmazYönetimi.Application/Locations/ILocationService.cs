namespace TasinmazYönetimi.Application.Locations;

public interface ILocationService
{
    Task<List<LocationResponse>> GetCitiesAsync(
        CancellationToken cancellationToken);

    Task<List<LocationResponse>> GetDistrictsAsync(
        int cityId, CancellationToken cancellationToken);

    Task<List<LocationResponse>> GetNeighborhoodsAsync(
        int districtId, CancellationToken cancellationToken);
}