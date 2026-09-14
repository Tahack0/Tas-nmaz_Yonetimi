using System.ComponentModel.DataAnnotations;

namespace TasinmazYönetimi.Application.Auth;

public class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(12)]
    public string Password { get; set; } = string.Empty;
}