using OrderHub.Application.Context;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Services;
using System.IO;
using System.Text.Json;

namespace OrderHub.Infrastructure.Services;
internal sealed class UserSessionStorage : IUserSessionStorage
{
    private readonly IApplicationDirectoriesService _directories;
    private readonly IEncryptionService _encryption;
    private readonly string _sessionPath;

    public UserSessionStorage(IApplicationDirectoriesService directories, IEncryptionService encryption)
    {
        _directories = directories;
        _encryption = encryption;
        _sessionPath = Path.Combine(_directories.StorageDirectory, "user-session.bin");
    }

    public void Save(UserSession session)
    {
        // serialize → encrypt → save
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(session);
        byte[] encryptedSession = _encryption.Encrypt(data);
        File.WriteAllBytes(_sessionPath, encryptedSession);
    }

    public UserSession Load()
    {
        if (!File.Exists(_sessionPath))
            return null;

        byte[] encryptedData = File.ReadAllBytes(_sessionPath);
        byte[] data = _encryption.Decrypt(encryptedData);

        return JsonSerializer.Deserialize<UserSession>(data);
    }

    public void Clear()
    {
        if (File.Exists(_sessionPath))
            File.Delete(_sessionPath);
    }
}