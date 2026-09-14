using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Application.Auth;

public record AuthenticatedUser(Guid Id, UserRole Role);