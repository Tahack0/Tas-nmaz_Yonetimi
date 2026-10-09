namespace TasinmazYönetimi.Domain.Entities;

// Çıkış yapılan token'ın yalnızca özetini saklar.
public class RevokedToken
{
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
