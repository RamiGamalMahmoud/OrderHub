using OrderHub.Application.Configuration;
using OrderHub.Application.Interfaces.Services;
using System;
using System.IO;
using System.Text.Json;

namespace OrderHub.Infrastructure.Configuration;

internal sealed class DatabaseConfigurationService
    : IDatabaseConfigurationService
{
    private readonly IApplicationDirectoriesService _directories;
    private readonly IEncryptionService _encryption;

    private string ConfigurationFilePath =>
        Path.Combine(_directories.StorageDirectory, "connection.bin");

    public DatabaseConfigurationService(
        IApplicationDirectoriesService directories,
        IEncryptionService encryption)
    {
        _directories = directories;
        _encryption = encryption;
    }

    public DatabaseSettings GetSettings()
    {
        if (!File.Exists(ConfigurationFilePath))
            return null;

        byte[] encryptedData =
            File.ReadAllBytes(ConfigurationFilePath);

        byte[] data =
            _encryption.Decrypt(encryptedData);

        return JsonSerializer.Deserialize<DatabaseSettings>(data)
            ?? throw new InvalidOperationException(
                "Invalid database configuration.");
    }

    public void SaveSettings(DatabaseSettings settings)
    {
        Directory.CreateDirectory(_directories.DataDirectory);

        byte[] data =
            JsonSerializer.SerializeToUtf8Bytes(settings);

        byte[] encryptedData =
            _encryption.Encrypt(data);

        File.WriteAllBytes(
            ConfigurationFilePath,
            encryptedData);
    }
}