using OrderHub.Application.Interfaces.Services;
using System;
using System.Security.Cryptography;

namespace OrderHub.Infrastructure.Services;
internal class HashShervice : IHashShervice
{
    private const int SALT_SIZE = 128 / 8;      // 16 bytes
    private const int KEY_SIZE = 256 / 8;       // 32 bytes
    private const int ITERATIONS = 100000;     // 100k iterations (adjustable)
    private static readonly HashAlgorithmName _hash_Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        // 1. Generate random salt
        byte[] salt = RandomNumberGenerator.GetBytes(SALT_SIZE);

        // 2. Hash with PBKDF2
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            ITERATIONS,
            _hash_Algorithm,
            KEY_SIZE
        );

        // 3. Combine salt + hash (for storage)
        byte[] combined = new byte[SALT_SIZE + KEY_SIZE];
        Buffer.BlockCopy(salt, 0, combined, 0, SALT_SIZE);
        Buffer.BlockCopy(hash, 0, combined, SALT_SIZE, KEY_SIZE);

        return Convert.ToBase64String(combined);
    }

    public bool Verify(string password, string storedHash)
    {
        try
        {
            byte[] combined = Convert.FromBase64String(storedHash);

            if (combined.Length != SALT_SIZE + KEY_SIZE)
                return false;

            byte[] salt = combined[..SALT_SIZE];
            byte[] hash = combined[SALT_SIZE..];

            byte[] testHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                ITERATIONS,
                _hash_Algorithm,
                KEY_SIZE);

            return CryptographicOperations.FixedTimeEquals(hash, testHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
