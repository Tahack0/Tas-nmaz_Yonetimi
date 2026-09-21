using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Application.Users;

public record UserResponse(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    DateTime CreatedAt);