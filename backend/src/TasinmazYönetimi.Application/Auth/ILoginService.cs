namespace TasinmazYönetimi.Application.Auth;

public interface ILoginService
{
    Task<AuthenticatedUser?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);
}