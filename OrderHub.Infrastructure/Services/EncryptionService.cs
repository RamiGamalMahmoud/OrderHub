using OrderHub.Application.Interfaces.Services;
using System.Security.Cryptography;

namespace OrderHub.Infrastructure.Services;

internal class EncryptionService : IEncryptionService
{
    private static readonly byte[] _key = null;

    public byte[] Encrypt(byte[] data)
        => ProtectedData.Protect(
            data,
            _key,
            DataProtectionScope.CurrentUser);

    public byte[] Decrypt(byte[] data)
        => ProtectedData.Unprotect(
            data,
            _key,
            DataProtectionScope.CurrentUser);
}