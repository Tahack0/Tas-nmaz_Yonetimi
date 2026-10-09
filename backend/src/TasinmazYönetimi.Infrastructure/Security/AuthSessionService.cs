using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TasinmazYönetimi.Application.Auth;
using TasinmazYönetimi.Application.Auditing;
using TasinmazYönetimi.Domain.Entities;
using TasinmazYönetimi.Domain.Enums;
using TasinmazYönetimi.Infrastructure.Persistence;

namespace TasinmazYönetimi.Infrastructure.Security;

public sealed class AuthSessionService(
    ApplicationDbContext db, IAuditLogService auditLogs) : IAuthSessionService
{
    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public Task<bool> IsRevokedAsync(string token, CancellationToken cancellationToken)
    {
        string hash = Hash(token);
        return db.Set<RevokedToken>().AnyAsync(
            item => item.TokenHash == hash, cancellationToken);
    }

    public async Task RevokeAsync(Guid userId, string token, DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        if (await IsRevokedAsync(token, cancellationToken)) return;

        db.Set<RevokedToken>().Add(new RevokedToken
        {
            TokenHash = Hash(token),
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        });
        auditLogs.Add(userId, LogStatus.Successful, OperationType.Logout,
            "Kullanıcı başarıyla çıkış yaptı.");

        try
        {
            // Token iptali ve çıkış logu birlikte kaydedilir.
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation,
              ConstraintName: "pk_revoked_tokens" })
        {
            // Aynı token için eşzamanlı ikinci çıkış ayrıca log oluşturmaz.
        }
    }
}
