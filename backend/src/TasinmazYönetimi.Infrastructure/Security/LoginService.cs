using Microsoft.EntityFrameworkCore;
using TasinmazYönetimi.Application.Auth;
using TasinmazYönetimi.Infrastructure.Persistence;

namespace TasinmazYönetimi.Infrastructure.Security;

public class LoginService : ILoginService
{
    private readonly ApplicationDbContext _db;

    public LoginService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AuthenticatedUser?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        string email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || !PasswordService.VerifyPassword(
                request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return null;
        }

        return new AuthenticatedUser(user.Id, user.Role);
    }
}