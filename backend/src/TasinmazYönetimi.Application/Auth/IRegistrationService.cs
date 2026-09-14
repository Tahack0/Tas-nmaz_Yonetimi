namespace TasinmazYönetimi.Application.Auth;

public interface IRegistrationService
{
    Task<Guid?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
}