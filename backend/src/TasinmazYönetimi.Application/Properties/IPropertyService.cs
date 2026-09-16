namespace TasinmazYönetimi.Application.Properties;

public interface IPropertyService
{
    Task<PropertyResponse> CreateAsync(
        Guid userId,
        CreatePropertyRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PropertyResponse>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<PropertyResponse?> GetByIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken);

    Task<PropertyResponse?> UpdateAsync(
        Guid userId,
        Guid id,
        UpdatePropertyRequest request,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken);
}