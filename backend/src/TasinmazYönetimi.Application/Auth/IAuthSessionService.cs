namespace TasinmazYönetimi.Application.Auth;

public interface IAuthSessionService
{
    Task<bool> IsRevokedAsync(string token, CancellationToken cancellationToken);
    Task RevokeAsync(Guid userId, string token, DateTime expiresAtUtc,
        CancellationToken cancellationToken);
}
