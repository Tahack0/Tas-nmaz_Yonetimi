using Microsoft.EntityFrameworkCore;
using TasinmazYönetimi.Application.Auditing;
using TasinmazYönetimi.Application.Common;
using TasinmazYönetimi.Application.Users;
using TasinmazYönetimi.Domain.Entities;
using TasinmazYönetimi.Domain.Enums;
using TasinmazYönetimi.Infrastructure.Persistence;
using TasinmazYönetimi.Infrastructure.Security;

namespace TasinmazYönetimi.Infrastructure.Users;

public sealed class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogService _auditLogs;

    public UserManagementService(
        ApplicationDbContext db,
        IAuditLogService auditLogs)
    {
        _db = db;
        _auditLogs = auditLogs;
    }

    public async Task<PagedResponse<UserResponse>> ListAsync(
        UserQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<User> users = _db.Users.AsNoTracking();

        int totalCount = await users.CountAsync(
            cancellationToken);

        int skip = (query.Page - 1) * query.PageSize;

        List<UserResponse> items = await users
            .OrderBy(user => user.Name)
            .ThenBy(user => user.Email)
            .Skip(skip)
            .Take(query.PageSize)
            .Select(user => new UserResponse(
                user.Id,
                user.Name,
                user.Email,
                user.Role,
                user.CreatedAt))
            .ToListAsync(cancellationToken);

        int totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(
                totalCount / (double)query.PageSize);

        return new PagedResponse<UserResponse>(
            items,
            query.Page,
            query.PageSize,
            totalCount,
            totalPages);
    }

    public async Task<UserResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(user => user.Id == id)
            .Select(user => new UserResponse(
                user.Id,
                user.Name,
                user.Email,
                user.Role,
                user.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserResponse> CreateAsync(
        Guid adminUserId,
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        string email =
            request.Email.Trim().ToLowerInvariant();

        bool emailExists = await _db.Users.AnyAsync(
            user => user.Email == email,
            cancellationToken);

        if (emailExists)
        {
            await AddFailedAuditAsync(
                adminUserId,
                OperationType.UserCreated,
                "Kullanıcı eklenemedi. E-posta adresi zaten kayıtlı.",
                cancellationToken);

            throw new ArgumentException(
                "Failed to add user. Please check all required fields.");
        }

        ValidateRole(request.Role);

        var (passwordHash, passwordSalt) =
            PasswordService.HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        _auditLogs.Add(
            adminUserId,
            LogStatus.Successful,
            OperationType.UserCreated,
            $"Kullanıcı eklendi. Kullanıcı Id: {user.Id}.");

        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(user);
    }

    public async Task<UserResponse?> UpdateAsync(
        Guid adminUserId,
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        User? user = await _db.Users.FirstOrDefaultAsync(
            user => user.Id == id,
            cancellationToken);

        if (user is null)
        {
            await AddFailedAuditAsync(
                adminUserId,
                OperationType.UserUpdated,
                $"Kullanıcı güncellenemedi. Kullanıcı Id: {id}.",
                cancellationToken);

            return null;
        }

        string email =
            request.Email.Trim().ToLowerInvariant();

        bool emailExists = await _db.Users.AnyAsync(
            existingUser =>
                existingUser.Email == email &&
                existingUser.Id != id,
            cancellationToken);

        if (emailExists)
        {
            await AddFailedAuditAsync(
                adminUserId,
                OperationType.UserUpdated,
                $"Kullanıcı güncellenemedi. Kullanıcı Id: {id}.",
                cancellationToken);

            throw new ArgumentException(
                "Failed to update user. Please check all required fields.");
        }

        ValidateRole(request.Role);

        var (passwordHash, passwordSalt) =
            PasswordService.HashPassword(request.Password);

        user.Name = request.Name.Trim();
        user.Email = email;
        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;
        user.Role = request.Role;
        user.UpdatedAt = DateTime.UtcNow;

        _auditLogs.Add(
            adminUserId,
            LogStatus.Successful,
            OperationType.UserUpdated,
            $"Kullanıcı güncellendi. Kullanıcı Id: {user.Id}.");

        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(user);
    }

    public async Task<bool> DeleteAsync(
        Guid adminUserId,
        Guid id,
        CancellationToken cancellationToken)
    {
        User? user = await _db.Users.FirstOrDefaultAsync(
            user => user.Id == id,
            cancellationToken);

        if (user is null)
        {
            await AddFailedAuditAsync(
                adminUserId,
                OperationType.UserDeleted,
                $"Kullanıcı silinemedi. Kullanıcı Id: {id}.",
                cancellationToken);

            return false;
        }

        _db.Users.Remove(user);

        _auditLogs.Add(
            adminUserId,
            LogStatus.Successful,
            OperationType.UserDeleted,
            $"Kullanıcı ve bağlı taşınmazları silindi. Kullanıcı Id: {id}.");

        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task AddFailedAuditAsync(
        Guid adminUserId,
        OperationType operationType,
        string description,
        CancellationToken cancellationToken)
    {
        _auditLogs.Add(
            adminUserId,
            LogStatus.Failed,
            operationType,
            description);

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateRole(UserRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentException(
                "Geçersiz kullanıcı rolü.");
        }
    }

    private static UserResponse ToResponse(User user) =>
        new(
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.CreatedAt);
}