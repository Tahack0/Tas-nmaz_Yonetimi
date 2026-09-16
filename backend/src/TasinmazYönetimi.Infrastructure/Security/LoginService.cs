using Microsoft.EntityFrameworkCore;
using TasinmazYönetimi.Application.Auditing;
using TasinmazYönetimi.Application.Auth;
using TasinmazYönetimi.Domain.Enums;
using TasinmazYönetimi.Infrastructure.Persistence;

namespace TasinmazYönetimi.Infrastructure.Security;

public class LoginService : ILoginService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogService _auditLogs;

    public LoginService(
        ApplicationDbContext db,
        IAuditLogService auditLogs)
    {
        _db = db;
        _auditLogs = auditLogs;
    }

    public async Task<AuthenticatedUser?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        string email = request.Email
            .Trim()
            .ToLowerInvariant();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Email == email,
                cancellationToken);

        bool passwordIsValid =
            user is not null &&
            PasswordService.VerifyPassword(
                request.Password,
                user.PasswordHash,
                user.PasswordSalt);

        if (!passwordIsValid)
        {
            _auditLogs.Add(
                userId: null,
                status: LogStatus.Failed,
                operationType: OperationType.Login,
                description: "Basarisiz giriş denemesi.");

            await _db.SaveChangesAsync(cancellationToken);

            return null;
        }

        _auditLogs.Add(
            userId: user!.Id,
            status: LogStatus.Successful,
            operationType: OperationType.Login,
            description: "Kullanici başariyla giriş yapti.");

        await _db.SaveChangesAsync(cancellationToken);

        return new AuthenticatedUser(
            user.Id,
            user.Role);
    }
}