using System.ComponentModel.DataAnnotations;
using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Application.Users;

public class UpdateUserRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(
    @"^(?=.*\p{L})(?=.*\d)(?=.*[^\p{L}\d\s])\S{8,12}$")]
    public string Password { get; set; } = string.Empty;

    [Required, EnumDataType(typeof(UserRole))]
    public UserRole Role { get; set; }
}