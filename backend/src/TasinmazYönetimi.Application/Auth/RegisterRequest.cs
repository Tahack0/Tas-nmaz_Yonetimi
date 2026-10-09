using System.ComponentModel.DataAnnotations;

namespace TasinmazYönetimi.Application.Auth;

public class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    // Kayıt parolasına ortak parola kuralını uygular.
    [Required]
    [RegularExpression(
        @"^(?=.*\p{L})(?=.*\d)(?=.*[^\p{L}\d\s])\S{8,12}$",
        ErrorMessage = "Parola 8–12 karakter olmalı; harf, rakam ve özel karakter içermeli.")]
    public string Password { get; set; } = string.Empty;
}