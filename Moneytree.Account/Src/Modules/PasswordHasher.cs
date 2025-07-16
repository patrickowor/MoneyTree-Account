namespace Moneytree.Account.Src.Modules;

using System;
using System.Security.Cryptography;

public static class PasswordHasher
{
    private static readonly string SecretKey = Environment.GetEnvironmentVariable("JWT_SECRET") ?? ""; // Store securely in production!

    public static string HashPassword(string password)
    {
        // Combine password and secret key (pepper)
        string combinedPassword = password + SecretKey;

        // Generate a unique salt
        byte[] salt = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);

        // Generate the hash
        using var pbkdf2 = new Rfc2898DeriveBytes(combinedPassword, salt, 100000, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(32); // 256-bit hash

        // Combine salt + hash
        byte[] hashBytes = new byte[48]; // 16 salt + 32 hash
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);

        return Convert.ToBase64String(hashBytes);
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        byte[] hashBytes = Convert.FromBase64String(storedHash);

        // Extract salt
        byte[] salt = new byte[16];
        Array.Copy(hashBytes, 0, salt, 0, 16);

        // Recompute hash using the same salt and secret key
        string combinedPassword = password + SecretKey;
        using var pbkdf2 = new Rfc2898DeriveBytes(combinedPassword, salt, 100000, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(32);

        // Compare stored hash with recomputed hash
        for (int i = 0; i < 32; i++)
            if (hashBytes[i + 16] != hash[i])
                return false;

        return true;
    }
}
