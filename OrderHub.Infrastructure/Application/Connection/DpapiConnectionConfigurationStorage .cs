using OrderHub.Application.Infrastructure.Connection;
using OrderHub.Application.Interfaces.Services;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Application.Connection;

public sealed class DpapiConnectionConfigurationStorage : IConnectionConfigurationStorage
{
    private readonly string _filePath;

    public DpapiConnectionConfigurationStorage(IApplicationDirectoriesService directoriesService)
    {
        _filePath = Path.Combine(directoriesService.StorageDirectory, "connection.bin");
    }

    public async Task SaveAsync(ConnectionConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(configuration);
        var plainBytes = Encoding.UTF8.GetBytes(json);

        var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.LocalMachine);

        await File.WriteAllBytesAsync(_filePath, encryptedBytes, cancellationToken);
    }

    public async Task<ConnectionConfiguration> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return null;

        var encryptedBytes = await File.ReadAllBytesAsync(_filePath, cancellationToken);

        try
        {
            var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.LocalMachine);
            var json = Encoding.UTF8.GetString(plainBytes);

            return JsonSerializer.Deserialize<ConnectionConfiguration>(json);
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);

        return Task.CompletedTask;
    }
}