using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal class FieTokenStorageService : ITokenStorageService
{
    private readonly IApplicationDirectoriesService _applicationDirectoriesService;
    private readonly IEncryptionService _encryptionService;

    public FieTokenStorageService(
        IApplicationDirectoriesService applicationDirectoriesService,
        IEncryptionService encryptionService)
    {
        _applicationDirectoriesService = applicationDirectoriesService;
        _encryptionService = encryptionService;
    }

    public Task ClearTokenAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<Token> GetTokenAsync()
    {
        string tokenFilePath = _applicationDirectoriesService.TokenFilePath;

        if (!File.Exists(tokenFilePath))
            return null;

        byte[] encryptedData =
            await File.ReadAllBytesAsync(tokenFilePath);

        byte[] data =
            _encryptionService.Decrypt(encryptedData);

        return JsonSerializer.Deserialize<Token>(data);
    }

    public async Task SaveTokenAsync(Token token)
    {
        byte[] data =
            JsonSerializer.SerializeToUtf8Bytes(token);

        byte[] encryptedData =
            _encryptionService.Encrypt(data);

        await File.WriteAllBytesAsync(
            _applicationDirectoriesService.TokenFilePath,
            encryptedData);
    }
}