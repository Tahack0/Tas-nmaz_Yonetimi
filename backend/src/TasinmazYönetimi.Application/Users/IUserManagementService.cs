using TasinmazYönetimi.Application.Common;

namespace TasinmazYönetimi.Application.Users;

public interface IUserManagementService
{
    Task<PagedResponse<UserResponse>> ListAsync(
        UserQuery query,
        CancellationToken cancellationToken);

    Task<UserResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<UserResponse> CreateAsync(
        Guid adminUserId,
        CreateUserRequest request,
        CancellationToken cancellationToken);

    Task<UserResponse?> UpdateAsync(
        Guid adminUserId,
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        Guid adminUserId,
        Guid id,
        CancellationToken cancellationToken);
}