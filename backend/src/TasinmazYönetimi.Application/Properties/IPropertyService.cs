using TasinmazYönetimi.Application.Common;
using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Application.Properties;

public interface IPropertyService
{
    Task<PropertyResponse> CreateAsync(
        Guid userId,
        CreatePropertyRequest request,
        CancellationToken cancellationToken);

    Task<PagedResponse<PropertyResponse>> ListAsync(
        Guid userId,
        UserRole userRole,
        PropertyQuery query,
        CancellationToken cancellationToken);

    Task<PropertyResponse?> GetByIdAsync(
        Guid userId,
        UserRole userRole,
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