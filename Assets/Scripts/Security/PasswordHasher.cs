using System;
using System.Security.Cryptography;

public static class PasswordHasher
{
    private const int SaltSize = 32;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty");

        byte[] salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        byte[] hash = PBKDF2(password, salt, Iterations, HashSize);

        byte[] combined = new byte[salt.Length + hash.Length];
        Array.Copy(salt, 0, combined, 0, salt.Length);
        Array.Copy(hash, 0, combined, salt.Length, hash.Length);

        return Convert.ToBase64String(combined);
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            return false;

        try
        {
            byte[] combined = Convert.FromBase64String(storedHash);

            if (combined.Length != SaltSize + HashSize)
                return false;

            byte[] salt = new byte[SaltSize];
            Array.Copy(combined, 0, salt, 0, SaltSize);

            byte[] storedPasswordHash = new byte[HashSize];
            Array.Copy(combined, SaltSize, storedPasswordHash, 0, HashSize);

            byte[] computedHash = PBKDF2(password, salt, Iterations, HashSize);

            return CryptographicOperations.FixedTimeEquals(computedHash, storedPasswordHash);
        }
        catch (FormatException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] PBKDF2(string password, byte[] salt, int iterations, int outputLength)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
        {
            return pbkdf2.GetBytes(outputLength);
        }
    }
}