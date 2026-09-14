using System.Security.Cryptography;

namespace TasinmazYönetimi.Infrastructure.Security;

public static class PasswordService
{
    private const int IterationCount = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static (string Hash, string Salt) HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            IterationCount,
            HashAlgorithmName.SHA256,
            HashSize);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool VerifyPassword(
        string password,
        string storedHash,
        string storedSalt)
    {
        if (string.IsNullOrEmpty(password) ||
            string.IsNullOrEmpty(storedHash) ||
            string.IsNullOrEmpty(storedSalt))
        {
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(storedSalt);
            byte[] expectedHash = Convert.FromBase64String(storedHash);

            if (salt.Length != SaltSize || expectedHash.Length != HashSize)
            {
                return false;
            }

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                IterationCount,
                HashAlgorithmName.SHA256,
                HashSize);

            return CryptographicOperations.FixedTimeEquals(
                actualHash,
                expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}