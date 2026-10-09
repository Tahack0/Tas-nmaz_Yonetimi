using System.ComponentModel.DataAnnotations;

namespace TasinmazYönetimi.Application.Auth;

public class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    // Kayıt parolasına ortak parola kuralını uygular.
    [Required(ErrorMessage = "Parola zorunludur.")]
    [RegularExpression(
        @"^(?=.*\p{L})(?=.*\d)(?=.*[^\p{L}\d\s])\S{8,12}$",
        ErrorMessage = "Parola 8–12 karakter olmalı; en az bir harf, bir rakam ve bir özel karakter içermeli; boşluk içermemeli.")]
    public string Password { get; set; } = string.Empty;
}
