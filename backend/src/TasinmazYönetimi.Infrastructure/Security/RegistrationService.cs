using Microsoft.EntityFrameworkCore;
using Npgsql;
using TasinmazYönetimi.Application.Auth;
using TasinmazYönetimi.Domain.Entities;
using TasinmazYönetimi.Domain.Enums;
using TasinmazYönetimi.Infrastructure.Persistence;

namespace TasinmazYönetimi.Infrastructure.Security;

public class RegistrationService : IRegistrationService
{
    private readonly ApplicationDbContext _db;

    public RegistrationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid?> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        string email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
            return null;

        var (hash, salt) = PasswordService.HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return null;
        }

        return user.Id;
    }
}